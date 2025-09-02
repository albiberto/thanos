using System.Numerics;
using Thanos.Common;
using Thanos.Memory;
using Thanos.War;

namespace Thanos.MCST;

public sealed class Worker
{
    private static readonly byte[] AllMovesArray = [Moves.Up, Moves.Down, Moves.Left, Moves.Right];
    private readonly NodeMemoryPool _nodePool;

    private readonly SlotMemoryPool _slotPool;

    private int _nextId;

    // Il costruttore non ha bisogno di essere una expression body per chiarezza
    public Worker(SlotMemoryPool slotPool, NodeMemoryPool nodePool)
    {
        _slotPool = slotPool;
        _nodePool = nodePool;

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
        var outcome = Simulate(leafIndex);
        
        // 4. BACKPROPAGATION: Propaga il risultato all'indietro.
        Backpropagate(leafIndex, outcome);
    }

    private int Select(int rootIndex)
    {
        var currentIndex = rootIndex;

        while (true)
        {
            ref var currentNode = ref _nodePool[currentIndex];

            // Condizione di terminazione: siamo arrivati a una foglia o a un nodo terminale.
            if (currentNode.IsLeafNode || currentNode.IsTerminal) return currentIndex;

            // 1. TROVA: il miglior figlio del nodo CORRENTE
            var candidateIndex = SelectBestChild(ref currentNode);

            if (candidateIndex == -1) throw new InvalidOperationException("SelectBestChild ha restituito -1 in un nodo non foglia.");

            // 2. AGGIORNA: l'indice e lascia che il ciclo continui per scendere al livello successivo
            currentIndex = candidateIndex;
        }
    }

    private int SelectBestChild(ref Node node, double explorationParameter = 1.41)
    {
        var bestScore = double.MinValue;
        var bestChildIndex = -1;

        var logParentVisits = Math.Log(node.Visits);

        var childIndex = node.FirstChildIndex;
        while (childIndex != -1)
        {
            ref var childNode = ref _nodePool[childIndex];

            if (childNode.Visits == 0) return childIndex;

            var exploitation = childNode.Wins / childNode.Visits;
            var exploration = Math.Sqrt(logParentVisits / childNode.Visits);
            var uctScore = exploitation + explorationParameter * exploration;

            if (uctScore > bestScore)
            {
                bestScore = uctScore;
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
        Console.WriteLine($"|-- Espansione Nodo {parentIndex}, Padre: {parentNode.ParentIndex} (raggiunto con mossa: {MoveToString(parentNode.MoveThatLedToThisNode)})");
        
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
        Console.WriteLine($"|   |-- Mosse valide: {MovesToString(legalMoves)}");
        
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
            childNode.Initialize(parentIndex, move, hash);

            // --- LOG: CREAZIONE FIGLIO ---
            // Stampa ogni figlio appena viene creato.
            Console.WriteLine($"|   |-- Creato figlio {childIndex} per la mossa {MoveToString(move)}");
            
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
    
    private double Simulate(int leafIndex)
    {
        // 1. Ottieni lo stato di partenza (una delle tue "fotografie" originali)
        var leafSlot = _slotPool[leafIndex];

        // 2. Prendi una sandbox e copia la fotografia lì dentro per non rovinarla
        var sandbox = _slotPool.GetSandBox(); 
        sandbox.CloneFrom(in leafSlot);      
        var arena = sandbox.Arena;
    
        // 3. Esegui il rollout sull'arena della sandbox
        const int turnLimit = 100;
        for (var i = 0; i < turnLimit; i++)
        {
            // La tua logica è corretta, ma possiamo semplificare i return
            var outcome = arena.Outcome();
            if (outcome != 0.0f) return outcome;
        
            var legalMovesMask = arena.GetLegalMoves();
            if (legalMovesMask == 0) return -1.0; // Sconfitta

            var move = RolloutMoveRandom(legalMovesMask);
            arena.ApplySingleMove(move);
        }
    
        // Se il loop finisce, usiamo un'euristica basata sullo stato finale
        return arena.Outcome(); // O una tua euristica, es: arena.Me.Length * 0.1
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

    private void Backpropagate(int startNodeIndex, double rawScore)
    {
        const double scalingFactor = 100.0;
        var normalizedResult = Math.Tanh(rawScore / scalingFactor);

        var currentIndex = startNodeIndex;
        while (currentIndex != -1)
        {
            ref var currentNode = ref _nodePool[currentIndex];
            currentNode.UpdateStats(normalizedResult);
            currentIndex = currentNode.ParentIndex;
        }
    }

    public void Reset(int newRootIndex) => _nextId = newRootIndex;
    
    // --- METODI HELPER PER LA STAMPA ---
    // Metti questi metodi di utilità da qualche parte nella tua classe Worker.

    private static string MoveToString(byte move) => move switch
    {
        Moves.Up => "Up",
        Moves.Down => "Down",
        Moves.Left => "Left",
        Moves.Right => "Right",
        Moves.None => "None", // Utile per il nodo radice
        _ => "Sconosciuta"
    };
    
    // Helper per stampare più mosse da una maschera di bit
    private static string MovesToString(byte moves)
    {
        if (moves == 0) return "Nessuna";
    
        var moveList = new List<string>();
        if ((moves & Moves.Up) != 0) moveList.Add("Up");
        if ((moves & Moves.Down) != 0) moveList.Add("Down");
        if ((moves & Moves.Left) != 0) moveList.Add("Left");
        if ((moves & Moves.Right) != 0) moveList.Add("Right");
    
        return string.Join('/', moveList);
    }
}