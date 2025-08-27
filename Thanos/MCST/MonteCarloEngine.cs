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
    Console.WriteLine(">>> Inizio FindBestMove con iterazioni max: {0}", iterations);

    // 2. Inizializza lo stato di gioco iniziale (rootSlot)
    var rootSlot = _warPool.GetNext(out _);
    rootSlot.InitializeFromRequest(in request);
    Console.WriteLine("RootSlot inizializzato da request. Head: {0}", rootSlot.GetArena.Snakes.Me.Head);

    // 3. Inizializza il nodo radice dell'albero
    var rootIndex = _nodePool.GetNextIndex();
    ref var rootNode = ref _nodePool[rootIndex];
    rootNode.Initialize(-1, Moves.None);
    Console.WriteLine("Nodo radice creato. Index: {0}", rootIndex);

    var counter = 1;

    // --- CICLO DI RICERCA MCTS ---
    var stopwatch = Stopwatch.StartNew();
    while (stopwatch.ElapsedMilliseconds < 450)
    {
        var workingSlot = _warPool.GetNext(out var full);
        if (full)
            Console.WriteLine("[WARN] WarMemoryPool pieno durante MCTS! Iterazione: {0}", counter);

        counter++;

        workingSlot.CloneFrom(in rootSlot);

        // Ottieni l'arena per questa iterazione
        var workingArena = workingSlot.GetArena;

        // --- FASE 1: SELEZIONE ---
        var selectedNodeIndex = Select(rootIndex, ref workingArena);
        ref var selectedNode = ref _nodePool[selectedNodeIndex];
        Console.WriteLine("[Iter {0}] Nodo selezionato: {1} (Move: {2})", counter, selectedNodeIndex, selectedNode.MoveThatLedToThisNode);

        // --- FASE 2: ESPANSIONE ---
        if (!selectedNode.IsTerminal && selectedNode.IsLeafNode)
        {
            float eval = workingArena.Evaluate();
            Console.WriteLine("[Iter {0}] Espansione: eval={1:F3}", counter, eval);

            if (eval == 0.0f)
            {
                Expand(selectedNodeIndex, ref selectedNode, ref workingArena);
                Console.WriteLine("[Iter {0}] Nodo espanso. Figlio: {1}", counter, selectedNode.FirstChildIndex);
            }
            else
            {
                selectedNode.IsTerminal = true;
                Console.WriteLine("[Iter {0}] Nodo marcato come terminale (eval != 0)", counter);
            }
        }

        // --- FASE 3: SIMULAZIONE ---
        var simulationResult = Simulate(ref workingArena);
        Console.WriteLine("[Iter {0}] Risultato simulazione: {1:F3}", counter, simulationResult);

        // --- FASE 4: BACKPROPAGATION ---
        Backpropagate(selectedNodeIndex, simulationResult);
    }

    Console.WriteLine(">>> MCTS completato in {0} ms con {1} iterazioni", stopwatch.ElapsedMilliseconds, counter);

    // --- SCELTA DELLA MOSSA MIGLIORE ---
    var bestChildIndex = -1;
    double bestWinRate = double.NegativeInfinity;

    ref var finalRootNode = ref _nodePool[rootIndex];

    if (finalRootNode.IsLeafNode)
    {
        var legalMoves = rootSlot.GetArena.Grid.GetLegalMoves(rootSlot.GetArena.Snakes.Me.Head);
        Console.WriteLine("[WARN] Nessun figlio espanso. Ritorno mossa legale di fallback.");
        return legalMoves != 0 ? (byte)(1 << BitOperations.TrailingZeroCount(legalMoves)) : Moves.Up;
    }

    Console.WriteLine(">>> Analisi finale dei figli:");
    foreach (var childIndex in finalRootNode.GetChildren(_nodePool))
    {
        ref var childNode = ref _nodePool[childIndex];
        if (childNode.Visits == 0) continue;

        double winRate = childNode.Wins / childNode.Visits;
        string moveName = childNode.MoveThatLedToThisNode switch
        {
            1 => "Up", 2 => "Down", 4 => "Left", 8 => "Right", _ => "?"
        };

        Console.WriteLine($"  - {moveName,-5} | WinRate: {winRate:F3} | Visits: {childNode.Visits}, Value: {childNode.Wins}");

        if (winRate > bestWinRate)
        {
            bestWinRate = winRate;
            bestChildIndex = childIndex;
        }
    }

    var bestMove = bestChildIndex != -1 ? _nodePool[bestChildIndex].MoveThatLedToThisNode : Moves.None;
    Console.WriteLine(">>> MOSSA SCELTA: {0} (WinRate: {1:F3})", bestMove, bestWinRate);

    return bestMove;
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
        const int turnLimit = 100; // Per i rollout, 100 turni sono già tanti

        for (var i = 0; i < turnLimit; i++)
        {
            // Controlla se il nostro serpente è morto o se è rimasto solo lui (vittoria)
            if (arena.Snakes.Me.Dead) return double.NegativeInfinity;
            // if (arena.Snakes.LiveSnakesCount <= 1) return double.PositiveInfinity;
        
            var legalMovesMask = arena.Grid.GetLegalMoves(arena.Snakes.Me.Head);

            if (legalMovesMask == 0) return double.NegativeInfinity;

            // Usa la policy di rollout, non l'euristica complessa!
            var move = Heuristics.SelectRolloutMove(legalMovesMask, ref arena);
        
            arena.ApplySingleMove(move);
        }

        // Se la simulazione finisce per limite di turni, usa l'euristica "pesante"
        // per giudicare la posizione finale.
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

    /// <summary>
    /// Stampa le statistiche dei figli della radice per capire il "pensiero" dell'albero MCTS.
    /// </summary>
    public void PrintTreeStats(in Request request, int iterations = 10000)
    {
        // Inizializza lo stato di gioco iniziale (rootSlot)
        var rootSlot = _warPool.GetNext(out _);
        rootSlot.InitializeFromRequest(in request);
        var rootIndex = _nodePool.GetNextIndex();
        ref var rootNode = ref _nodePool[rootIndex];
        rootNode.Initialize(-1, Moves.None);
        // Esegui MCTS
        var counter = 1;
        var stopwatch = Stopwatch.StartNew();
        while (stopwatch.ElapsedMilliseconds < 450)
        {
            var workingSlot = _warPool.GetNext(out var full);
            counter++;
            workingSlot.CloneFrom(in rootSlot);
            var workingArena = workingSlot.GetArena;
            var selectedNodeIndex = Select(rootIndex, ref workingArena);
            ref var selectedNode = ref _nodePool[selectedNodeIndex];
            if (!selectedNode.IsTerminal && selectedNode.IsLeafNode)
            {
                if (workingArena.Evaluate() == 0.0f)
                    Expand(selectedNodeIndex, ref selectedNode, ref workingArena);
                else
                    selectedNode.IsTerminal = true;
            }
            var simulationResult = Simulate(ref workingArena);
            Backpropagate(selectedNodeIndex, simulationResult);
        }
        // Stampa le statistiche dei figli della radice
        ref var finalRootNode = ref _nodePool[rootIndex];
        Console.WriteLine("--- STATISTICHE ALBERO MCTS ---");
        foreach (var childIndex in finalRootNode.GetChildren(_nodePool))
        {
            ref var childNode = ref _nodePool[childIndex];
            if (childNode.Visits == 0) continue;
            double winRate = childNode.Wins / childNode.Visits;
            string moveName = childNode.MoveThatLedToThisNode switch { 1 => "Up", 2 => "Down", 4 => "Left", 8 => "Right", _ => "?" };
            Console.WriteLine($"Mossa: {moveName,-5} | Win Rate: {winRate:F3} | Value: {childNode.Wins} | Visits: {childNode.Visits}");
        }
        Console.WriteLine("------------------------------");
    }
}