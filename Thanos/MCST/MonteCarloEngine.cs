using System.Diagnostics;
using System.Numerics;
using Thanos.Common;
using Thanos.Memory;
using Thanos.SourceGen;
using Thanos.War;

namespace Thanos.MCST;

public class MonteCarloEngine(WarMemoryPool warPool, NodeMemoryPool nodePool)
{
    private readonly WarMemoryPool _warPool = warPool;
    private NodeMemoryPool _nodePool = nodePool;

    public byte FindBestMove(in Request request, int iterations = 10000)
    {
        // 2. Inizializza lo stato di gioco iniziale (rootSlot)
        var rootSlot = _warPool.GetNext(out _);
        rootSlot.InitializeFromRequest(in request);

        // 3. Inizializza il nodo radice dell'albero
        var rootIndex = _nodePool.GetNextIndex();
        ref var rootNode = ref _nodePool[rootIndex];
        rootNode.Initialize(-1, Moves.None);

        var counter = 1;

        // --- CICLO DI RICERCA MCTS ---
        var stopwatch = Stopwatch.StartNew();
        while (stopwatch.ElapsedMilliseconds < 450)
            // while (counter < 1000)
        {
            // Per ogni iterazione, partiamo sempre dallo stato originale della radice
            var workingSlot = _warPool.GetNext(out var full);

            if (full) Console.WriteLine("WarMemoryPool pieno durante MCTS! {0}", counter);

            counter++;

            workingSlot.CloneFrom(in rootSlot);

            // Ottieni l'arena per questa iterazione. È una 'ref struct', quindi vive sullo stack.
            var workingArena = workingSlot.GetArena;

            // --- FASE 1: SELEZIONE ---
            // Passiamo 'workingArena' con 'ref' in modo che 'Select' possa modificarla
            // e le modifiche siano visibili alle fasi successive.
            var selectedNodeIndex = Select(rootIndex, ref workingArena);
            ref var selectedNode = ref _nodePool[selectedNodeIndex];

            // --- FASE 2: ESPANSIONE ---
            if (!selectedNode.IsTerminal && selectedNode.IsLeafNode)
            {
                // Valutiamo l'arena *dopo* che è stata modificata dalla fase di Selezione
                if (workingArena.Evaluate() == 0.0f)
                    Expand(selectedNodeIndex, ref selectedNode, ref workingArena);
                else
                    selectedNode.IsTerminal = true;
            }

            // --- FASE 3: SIMULAZIONE (ROLLOUT) ---
            // La simulazione parte dallo stato raggiunto DOPO selezione ed eventuale espansione
            var simulationResult = Simulate(ref workingArena);

            // --- FASE 4: BACKPROPAGATION ---
            Backpropagate(selectedNodeIndex, simulationResult);
        }

        Console.WriteLine("MCTS completato in {0} ms con {1} iterazioni", stopwatch.ElapsedMilliseconds, counter);

        // --- SCELTA DELLA MOSSA MIGLIORE ---
        // Questa parte era già corretta: scegli il figlio più visitato.
        var bestChildIndex = -1;
        double bestWinRate = double.NegativeInfinity;

        ref var finalRootNode = ref _nodePool[rootIndex];

// Se non ci sono figli, restituisci una mossa di default (improbabile ma sicuro)
        if (finalRootNode.IsLeafNode)
        {
            // Cerca la prima mossa legale e restituiscila
            var legalMoves = rootSlot.GetArena.Grid.GetLegalMoves(rootSlot.GetArena.Snakes.Me.Head);
            return legalMoves != 0 ? (byte)(1 << BitOperations.TrailingZeroCount(legalMoves)) : Moves.Up;
        }


        foreach (var childIndex in finalRootNode.GetChildren(_nodePool))
        {
            ref var childNode = ref _nodePool[childIndex];
    
            // Non considerare mai figli non visitati
            if (childNode.Visits == 0) continue;

            // Calcola il "win rate" (punteggio medio per visita)
            double winRate = childNode.Wins / childNode.Visits;
    
            // Debug: Stampa le statistiche di ogni opzione finale
            string moveName = childNode.MoveThatLedToThisNode switch { 1 => "Up", 2 => "Down", 4 => "Left", 8 => "Right", _ => "?" };
            Console.WriteLine(
                $"OPZIONE FINALE: {moveName,-5} | Win Rate: {winRate:F3} " +
                $"(Value: {childNode.Wins}, Visits: {childNode.Visits})"
            );

            if (winRate > bestWinRate)
            {
                bestWinRate = winRate;
                bestChildIndex = childIndex;
            }
        }

        return bestChildIndex != -1 ? _nodePool[bestChildIndex].MoveThatLedToThisNode : Moves.None;
    }

    /// <summary>
    ///     FASE 1: Scende l'albero partendo da un indice, scegliendo i figli più promettenti (UCT)
    ///     e aggiornando lo stato di gioco ('workingArena') di conseguenza.
    ///     Restituisce l'indice del nodo foglia selezionato.
    /// </summary>
    private int Select(int startNodeIndex, ref WarArena arena) // <-- BUG #2 RISOLTO: aggiunto 'ref'
    {
        var currentIndex = startNodeIndex;

        while (true)
        {
            ref var currentNode = ref _nodePool[currentIndex];
            if (currentNode.IsLeafNode || currentNode.IsTerminal) return currentIndex;

            var nextNodeIndex = currentNode.SelectBestChild(_nodePool);

            // Non ci sono più figli da esplorare da questo ramo.
            // La selezione termina qui, restituendo il nodo attuale.
            if (nextNodeIndex == -1) return currentIndex;

            currentIndex = nextNodeIndex;

            ref var childNode = ref _nodePool[currentIndex];
            arena.ApplySingleMove(childNode.MoveThatLedToThisNode);
        }
    }

    /// <summary>
    ///     FASE 2: Crea i nodi figli per un dato nodo foglia usando un ciclo bitwise diretto.
    /// </summary>
    private void Expand(int nodeIndex, ref Node node, ref WarArena arena)
    {
        if (node.IsTerminal) return;

        // 1. Ottieni la bitmask come prima
        var legalMovesMask = arena.Grid.GetLegalMoves(arena.Snakes.Me.Head);

        // Se non ci sono mosse, il nodo è di fatto un nodo terminale
        if (legalMovesMask == 0)
        {
            node.IsTerminal = true;
            return;
        }

        // 2. Cicla sulle possibili mosse, creando i figli man mano che le trovi
        var lastChildIndex = -1;

        // Per rendere il codice leggibile, iteriamo su un array di mosse possibili
        ReadOnlySpan<byte> allMoves = [Moves.Up, Moves.Down, Moves.Left, Moves.Right];

        foreach (var move in allMoves)
        {
            // Controlla se la mossa corrente è presente nella maschera
            if ((legalMovesMask & move) == 0) continue;

            // È una mossa legale, quindi creiamo il figlio
            var newChildIndex = _nodePool.GetNextIndex();
            ref var childNode = ref _nodePool[newChildIndex];
            childNode.Initialize(nodeIndex, move);

            // Ora dobbiamo collegare il nuovo figlio alla lista dei fratelli
            if (lastChildIndex == -1)
            {
                // Se è il primo figlio che troviamo, lo colleghiamo direttamente al genitore
                node.FirstChildIndex = newChildIndex;
            }
            else
            {
                // Altrimenti, lo colleghiamo al fratello precedente
                ref var lastChildNode = ref _nodePool[lastChildIndex];
                lastChildNode.NextSiblingIndex = newChildIndex;
            }

            // Aggiorniamo l'indice dell'ultimo figlio creato per il prossimo giro
            lastChildIndex = newChildIndex;
        }
    }

// All'interno della tua classe MonteCarloEngine

/// <summary>
///     FASE 3: Da uno stato di gioco, esegue una partita ("rollout") fino a un
///     risultato terminale, restituendo il punteggio (-1 per sconfitta, 1 per vittoria).
/// </summary>
// File: MonteCarloEngine.cs
    private double Simulate(ref WarArena arena)
    {
        const int turnLimit = 200; // Ridotto il limite per simulazioni più veloci

        for (var i = 0; i < turnLimit; i++)
        {
            // 1. Controlla se la partita è terminata (vittoria/sconfitta)
            var evaluation = arena.Evaluate(); // Usa la Evaluate di WarArena per stati terminali
            if (evaluation != 0.0f) return evaluation;

            // 2. Ottieni le mosse legali
            var legalMovesMask = arena.Grid.GetLegalMoves(arena.Snakes.Me.Head);

            if (legalMovesMask == 0) return -1.0f; // Se non ci sono mosse, è una sconfitta in simulazione

            // 3. Usa la nostra nuova euristica per scegliere la mossa migliore
            //    invece di una mossa casuale.
            var move = Heuristics.FindBestMove(legalMovesMask, ref arena);

            // 4. Applica la mossa per far avanzare lo stato
            arena.ApplySingleMove(move);
        }

        // Se si raggiunge il limite, consideralo un pareggio o valuta lo stato finale
        // con la nostra euristica per un risultato più sfumato.
        return Heuristics.Evaluate(ref arena);
    }

    /// <summary>
    ///     FASE 4: Propaga il risultato della simulazione a ritroso lungo l'albero,
    ///     aggiornando le statistiche (vittorie/visite) di ogni nodo attraversato.
    /// </summary>
    private void Backpropagate(int startNodeIndex, double rawScore)
    {
        // --- NORMALIZZAZIONE ---
        // Convertiamo il punteggio grezzo (es. 19746.0) in un valore semplice
        // che l'albero può capire:
        // - Punteggio positivo => Buona posizione (+1.0)
        // - Punteggio negativo => Cattiva posizione (-1.0)
        // - Punteggio zero     => Neutrale (0.0)
        var normalizedResult = (double)Math.Sign(rawScore);

        Console.WriteLine("Raw Score: {0}, Normalized: {1}", rawScore, normalizedResult);
        
        var currentIndex = startNodeIndex;
        while (currentIndex != -1)
        {
            ref var currentNode = ref _nodePool[currentIndex];
        
            // Passiamo il valore normalizzato al nodo
            currentNode.UpdateStats(normalizedResult); 

            currentIndex = currentNode.ParentIndex;
        }
    }
}