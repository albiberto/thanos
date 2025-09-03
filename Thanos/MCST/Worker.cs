using System.Numerics;
using Thanos.Common;
using Thanos.Memory;
using Thanos.PreWarm.Memory;
using Thanos.War;

namespace Thanos.MCST;

public sealed class Worker
{
    private static readonly byte[] AllMovesArray = [Moves.Up, Moves.Down, Moves.Left, Moves.Right];
    private readonly NodeMemoryPool _nodePool;

    private readonly SlotMemoryPool _slotPool;
    private readonly Luts _luts;

    private int _nextId;
    
    private const double EXPLORATION_PARAMETER = 1.41; // Il classico C per UCT
    private const double HEURISTIC_WEIGHT = 0.5;       // Peso per l'euristica in selezione

    // Il costruttore non ha bisogno di essere una expression body per chiarezza
    public Worker(SlotMemoryPool slotPool, NodeMemoryPool nodePool, Luts luts)
    {
        _slotPool = slotPool;
        _nodePool = nodePool;
        
        _luts = luts;

        _nextId = 1;
    }

    private int AllocateNextId() => _nextId++;

    public void RunIteration(int rootIndex)
    {
        // 1. SELECTION: Trova un nodo foglia da cui partire.
        var leafIndex = Select(rootIndex);
        ref var leafNode = ref _nodePool[leafIndex];

        // 2. EXPANSION: Se il nodo è nuovo e non terminale, crea i suoi figli.
        if (leafNode is { IsLeafNode: true, IsTerminal: false })
        {
            Expand(leafIndex);
            // Potremmo decidere di scendere in uno dei nuovi figli per la simulazione,
            // ma per semplicità partiamo dalla foglia originale.
        }

        // 3. SIMULATION: Esegui un rollout partendo dallo stato del nodo foglia.
        var outcome = Evaluate(leafIndex);
        
        // 4. BACKPROPAGATION: Propaga il risultato all'indietro.
        Backpropagate(leafIndex, outcome);
    }

        private int Select(int rootIndex)
    {
        var currentIndex = rootIndex;
        while (true)
        {
            ref var currentNode = ref _nodePool[currentIndex];
            if (currentNode.IsLeafNode || currentNode.IsTerminal) return currentIndex;

            // La chiamata non cambia, ma il comportamento del metodo chiamato sì.
            var candidateIndex = SelectBestChild(ref currentNode);

            if (candidateIndex == -1) throw new InvalidOperationException("SelectBestChild ha restituito -1 in un nodo non foglia.");

            currentIndex = candidateIndex;
        }
    }

    /// <summary>
    /// Seleziona il figlio migliore usando una formula UCT potenziata dall'euristica.
    /// </summary>
    private int SelectBestChild(ref Node parentNode)
    {
        var bestScore = double.MinValue;
        var bestChildIndex = -1;

        // Se il nodo genitore è stato visitato, possiamo calcolare il termine di esplorazione.
        // Se non è mai stato visitato (Visits=0), il log darebbe errore, ma questa condizione
        // è già gestita dal fatto che un nodo con 0 visite non può avere figli visitati.
        var logParentVisits = Math.Log(parentNode.Visits);

        var childIndex = parentNode.FirstChildIndex;
        while (childIndex != -1)
        {
            ref var childNode = ref _nodePool[childIndex];

            // Un figlio mai visitato ha sempre la priorità assoluta.
            if (childNode.Visits == 0) return childIndex;

            // --- 1. Calcolo UCT Standard ---
            var exploitation = childNode.Wins / childNode.Visits;
            var exploration = EXPLORATION_PARAMETER * Math.Sqrt(logParentVisits / childNode.Visits);
            var uctScore = exploitation + exploration;

            // --- 2. Aggiunta del Termine Euristico ---
            // Otteniamo lo stato del figlio per poterlo valutare.
            var childArena =  _slotPool[childIndex].Arena;
            
            // Creiamo e usiamo l'euristica per ottenere un punteggio "a priori".
            var heuristics = new Heuristics(childArena.Grid, childArena.Me, childArena.Enemies, _luts.ConversionsMap, _luts.PositionalScores);
            var heuristicScore = heuristics.Evaluate();
            
            // Normalizziamo il punteggio euristico con Tanh per mantenerlo in un range [-1, 1]
            // ed evitare che domini completamente la formula UCT.
            var normalizedHeuristic = Math.Tanh(heuristicScore / 100.0);

            // --- 3. Calcolo del Punteggio Finale ---
            // Il punteggio finale è una combinazione di UCT e della "sensazione" dell'euristica.
            var finalScore = uctScore + HEURISTIC_WEIGHT * normalizedHeuristic;

            if (finalScore > bestScore)
            {
                bestScore = finalScore;
                bestChildIndex = childIndex;
            }

            childIndex = childNode.NextSiblingIndex;
        }

        return bestChildIndex;
    }

    private void Expand(int parentIndex)
    {
        // 1. PREPARA I DATI DEL NODO PADRE
        ref var parentNode = ref _nodePool[parentIndex];
        var parentSlot = _slotPool[parentIndex];
        var parentArena = parentSlot.Arena;

        // --- LOG: INIZIO ESPANSIONE ---
        // Stampa il nodo che stiamo per espandere.
        // Console.WriteLine($"|-- Espansione Nodo {parentIndex}, Padre: {parentNode.ParentIndex} (raggiunto con mossa: {MoveToString(parentNode.MoveThatLedToThisNode)})");
        
        // 2. CONTROLLI PRELIMINARI-
        if (parentArena.GameOver)
        {
            parentNode.IsTerminal = true;
            return;
        }

        // 3. CALCOLA LE MOSSE POSSIBILI
        var legalMoves = parentArena.GetLegalMoves();
        
        // --- LOG: MOSSE VALIDE ---
        // Stampa le mosse che verranno usate per creare i figli.
        // Console.WriteLine($"|   |-- Mosse valide: {MovesToString(legalMoves)}");
        
        if (legalMoves == 0)
        {
            parentNode.IsTerminal = true;
            return;
        }

        // 4. CREA I NODI FIGLI
        var lastChildIndex = -1;
        foreach (var move in AllMovesArray)
        {
            if ((legalMoves & move) == 0) continue;

            // --- Alloca un INDEX unificato per il nuovo figlio ---
            var childIndex = AllocateNextId();

            // --- a. Usa INDEX per preparare lo stato del figlio ---
            var childSlot = _slotPool[childIndex];
            childSlot.CloneFrom(in parentSlot);
            var arena = childSlot.Arena;
            arena.ApplySingleMove(move);

            var hash = ZobristHasher.CalculateHash(in arena);

            // --- b. Usa LO STESSO INDEX per preparare il nodo del figlio ---
            ref var childNode = ref _nodePool[childIndex];
            childNode.PlacementNew(parentIndex, move, hash);

            // --- LOG: CREAZIONE FIGLIO ---
            // Stampa ogni figlio appena viene creato.
            // Console.WriteLine($"|   |-- Creato figlio {childIndex} per la mossa {MoveToString(move)}");
            
            // --- c. Collega il nuovo figlio all'albero ---
            if (lastChildIndex == -1)
            {
                parentNode.FirstChildIndex = childIndex;
            }
            else
            {
                ref var lastChildNode = ref _nodePool[lastChildIndex];
                lastChildNode.NextSiblingIndex = childIndex;
            }

            lastChildIndex = childIndex;
        }
    }
    
    /// <summary>
    /// Valuta un nodo foglia usando l'euristica. Sostituisce la simulazione casuale.
    /// </summary>
    private float Evaluate(int leafIndex) // <-- Metodo rinominato
    {
        // 1. Ottieni lo stato del nodo da valutare.
        var arena = _slotPool[leafIndex].Arena;

        var outcome = arena.Outcome();
        if (outcome != 0.0f) return outcome;

        // 2. Crea l'oggetto Heuristics e valuta lo stato.
        var heuristics = new Heuristics(arena.Grid, arena.Me, arena.Enemies, _luts.ConversionsMap, _luts.PositionalScores);
    
        // 3. Il punteggio dell'euristica è il risultato.
        return heuristics.Evaluate();
    }

    private static byte RolloutMoveRandom(byte legalMoves)
    {
        if (legalMoves == 0) return Moves.Up;
        if (BitOperations.IsPow2(legalMoves)) return legalMoves;

        var count = BitOperations.PopCount(legalMoves);
        var randomIndex = Random.Shared.Next(count);

        byte move = 0;
        for (var i = 0; i <= randomIndex; i++)
        {
            move = (byte)(1 << BitOperations.TrailingZeroCount(legalMoves));
            legalMoves &= (byte)~move;
        }

        return move;
    }

    private void Backpropagate(int startNodeIndex, float rawScore)
    {
        const float scalingFactor = 100.0f;
        var normalizedResult = MathF.Tanh(rawScore / scalingFactor);

        var currentIndex = startNodeIndex;
        while (currentIndex != -1)
        {
            ref var currentNode = ref _nodePool[currentIndex];
            currentNode.UpdateStats(normalizedResult);
            currentIndex = currentNode.ParentIndex;
        }
    }
    
    public void Reset(int startId) => _nextId = startId;
}