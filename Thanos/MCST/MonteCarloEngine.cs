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
    private readonly NodeMemoryPool _nodePool = nodePool;

    public byte FindBestMove(in Request request)
    {
        // 1. Inizializza lo stato di gioco iniziale (rootSlot)
        var rootSlot = _warPool.GetNext(out _);
        rootSlot.InitializeFromRequest(in request);

        // 2. Inizializza il nodo radice dell'albero
        var rootIndex = _nodePool.GetNextIndex();
        ref var rootNode = ref _nodePool[rootIndex];
        rootNode.Initialize(-1, Moves.None);

        var counter = 1;

        // --- CICLO DI RICERCA MCTS ---
        var stopwatch = Stopwatch.StartNew();
        while (stopwatch.ElapsedMilliseconds < 450)
        {
            var workingSlot = _warPool.GetNext(out _);

            counter++;

            workingSlot.CloneFrom(in rootSlot);

            // Ottieni l'arena per questa iterazione
            var workingArena = workingSlot.GetArena;

            // --- FASE 1: SELEZIONE ---
            var selectedNodeIndex = Select(rootIndex, ref workingArena);
            ref var selectedNode = ref _nodePool[selectedNodeIndex];

            // --- FASE 2: ESPANSIONE ---
            if (selectedNode is { IsTerminal: false, IsLeafNode: true })
            {
                var eval = workingArena.Evaluate();

                if (eval == 0.0f) 
                    Expand(selectedNodeIndex, ref selectedNode, ref workingArena);
                else
                    selectedNode.IsTerminal = true;
            }

            // --- FASE 3: SIMULAZIONE ---
            var result = Simulate(ref workingArena);

            // --- FASE 4: BACKPROPAGATION ---
            Backpropagate(selectedNodeIndex, result);
        }

        Console.WriteLine(">>> MCTS completato in {0} ms con {1} iterazioni", stopwatch.ElapsedMilliseconds, counter);

        // --- SCELTA DELLA MOSSA MIGLIORE ---
        var bestChildIndex = -1;
        var bestWinRate = double.NegativeInfinity;

        ref var finalRootNode = ref _nodePool[rootIndex];

        if (finalRootNode.IsLeafNode)
        {
            var legalMoves = rootSlot.GetArena.Grid.GetLegalMoves(rootSlot.GetArena.Snakes.Me.Head);
            return legalMoves != 0 ? (byte)(1 << BitOperations.TrailingZeroCount(legalMoves)) : Moves.Up;
        }

        foreach (var childIndex in finalRootNode.GetChildren(_nodePool))
        {
            ref var childNode = ref _nodePool[childIndex];
            if (childNode.Visits == 0) continue;

            var winRate = childNode.Wins / childNode.Visits;
            if (winRate > bestWinRate)
            {
                bestWinRate = winRate;
                bestChildIndex = childIndex;
            }
        }

        var bestMove = bestChildIndex != -1 ? _nodePool[bestChildIndex].MoveThatLedToThisNode : Moves.None;

        return bestMove;
    }

    /// <summary>
    ///     FASE 1: Scende l'albero partendo da un indice, scegliendo i figli più promettenti (UCT)
    ///     e aggiornando lo stato di gioco ('workingArena') di conseguenza.
    ///     Restituisce l'indice del nodo foglia selezionato.
    /// </summary>
    private int Select(int startNodeIndex, ref WarArena arena)
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

        // Console.WriteLine("Raw Score: {0}, Normalized: {1}", rawScore, normalizedResult);

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