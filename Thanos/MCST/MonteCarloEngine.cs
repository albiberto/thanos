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

    // <--- OTTIMIZZAZIONE: Definiamo l'array una sola volta per evitare allocazioni nel ciclo.
    private static readonly byte[] AllMovesArray = [Moves.Up, Moves.Down, Moves.Left, Moves.Right];

    public byte FindBestMove(in Request request)
    {
        var rootSlot = _warPool.GetNext(out _);
        rootSlot.InitializeFromRequest(in request);

        var rootIndex = _nodePool.GetNextIndex();
        ref var rootNode = ref _nodePool[rootIndex];
        rootNode.Initialize(-1, Moves.None);

        var counter = 1;

        var stopwatch = Stopwatch.StartNew();
        while (stopwatch.ElapsedMilliseconds < 450)
        {
            var workingSlot = _warPool.GetNext(out _);
            counter++;
            workingSlot.CloneFrom(in rootSlot);
            var workingArena = workingSlot.GetArena;

            // --- FASE 1: SELEZIONE ---
            var nodeToProcessIndex = Select(rootIndex, ref workingArena);
            ref var nodeToProcess = ref _nodePool[nodeToProcessIndex];
            
            // <--- CORREZIONE: Flusso logico di Espansione e Simulazione corretto
            double simulationResult;

            // --- FASE 2 & 3: ESPANSIONE E SIMULAZIONE ---
            // Controlla lo stato DOPO essere arrivati al nodo selezionato
            if (workingArena.Snakes.Me.Dead)// || workingArena.Snakes.LiveSnakesCount <= 1)
            {
                // Se siamo arrivati in uno stato terminale, non c'è bisogno di simulare
                nodeToProcess.IsTerminal = true;
                simulationResult = Heuristics.Evaluate(ref workingArena);
            }
            else if (nodeToProcess.IsLeafNode)
            {
                // Se è una foglia, la espandiamo
                Expand(nodeToProcessIndex, ref nodeToProcess, ref workingArena);

                // Scegliamo un figlio casuale da cui partire per la simulazione
                var firstChildIndex = nodeToProcess.FirstChildIndex;
                if (firstChildIndex != -1)
                {
                    // Avanziamo lo stato al primo figlio e iniziamo la simulazione da lì
                    ref var childNode = ref _nodePool[firstChildIndex];
                    workingArena.ApplySingleMove(childNode.MoveThatLedToThisNode);
                    nodeToProcessIndex = firstChildIndex; // Il risultato partirà da questo figlio
                }
                
                // Ora eseguiamo la simulazione partendo da questo nuovo stato
                simulationResult = Simulate(ref workingArena);
            }
            else
            {
                // Se il nodo non è una foglia e non è terminale, allora siamo bloccati 
                // in un ramo già esplorato. Eseguiamo solo la simulazione da qui.
                simulationResult = Simulate(ref workingArena);
            }

            // --- FASE 4: BACKPROPAGATION ---
            Backpropagate(nodeToProcessIndex, simulationResult);
        }

        Console.WriteLine(">>> MCTS completato in {0} ms con {1} iterazioni", stopwatch.ElapsedMilliseconds, counter);
        
        // --- SCELTA DELLA MOSSA MIGLIORE ---
        ref var finalRootNode = ref _nodePool[rootIndex];
        
        // <--- OTTIMIZZAZIONE: Usa SelectBestChild con esplorazione a zero per la scelta finale.
        // Questo è più robusto e riutilizza la logica UCT in modalità "solo sfruttamento".
        var bestChildIndex = finalRootNode.SelectBestChild(_nodePool, 0.0); // Con 0.0, l'esplorazione è disattivata

        // Log finale per il debug
        Console.WriteLine(">>> Analisi finale dei figli:");
        foreach (var childIndex in finalRootNode.GetChildren(_nodePool))
        {
            ref var childNode = ref _nodePool[childIndex];
            if (childNode.Visits == 0) continue;
            var winRate = childNode.Wins / childNode.Visits;
            string moveName = childNode.MoveThatLedToThisNode switch { 1 => "Up", 2 => "Down", 4 => "Left", 8 => "Right", _ => "?" };
            Console.WriteLine($"  - {moveName,-5} | WinRate: {winRate:F3} | Visits: {childNode.Visits}, Value: {childNode.Wins}");
        }

        // Se per qualche motivo nessun figlio è stato visitato, fai una mossa di emergenza
        if (bestChildIndex != -1) return _nodePool[bestChildIndex].MoveThatLedToThisNode;
        
        var legalMoves = rootSlot.GetArena.Grid.GetLegalMoves(rootSlot.GetArena.Snakes.Me.Head);
        return legalMoves != 0 ? (byte)(1 << BitOperations.TrailingZeroCount(legalMoves)) : Moves.Up;

    }

    private int Select(int startNodeIndex, ref WarArena arena)
    {
        var currentIndex = startNodeIndex;
        while (true)
        {
            ref var currentNode = ref _nodePool[currentIndex];
            if (currentNode.IsLeafNode || currentNode.IsTerminal) return currentIndex;
            
            // <--- NOTA: Assicurati che SelectBestChild usi un fattore di esplorazione > 0
            var nextNodeIndex = currentNode.SelectBestChild(_nodePool);
            
            if (nextNodeIndex == -1) return currentIndex;

            currentIndex = nextNodeIndex;
            ref var childNode = ref _nodePool[currentIndex];
            arena.ApplySingleMove(childNode.MoveThatLedToThisNode);
        }
    }

    private void Expand(int nodeIndex, ref Node node, ref WarArena arena)
    {
        if (node.IsTerminal) return;
        
        var legalMovesMask = arena.Grid.GetLegalMoves(arena.Snakes.Me.Head);
        if (legalMovesMask == 0)
        {
            node.IsTerminal = true;
            return;
        }
        
        var lastChildIndex = -1;
        
        // <--- OTTIMIZZAZIONE: Usa l'array statico per non allocare memoria.
        foreach (var move in AllMovesArray)
        {
            if ((legalMovesMask & move) == 0) continue;

            var newChildIndex = _nodePool.GetNextIndex();
            ref var childNode = ref _nodePool[newChildIndex];
            childNode.Initialize(nodeIndex, move);

            if (lastChildIndex == -1)
            {
                node.FirstChildIndex = newChildIndex;
            }
            else
            {
                ref var lastChildNode = ref _nodePool[lastChildIndex];
                lastChildNode.NextSiblingIndex = newChildIndex;
            }
            lastChildIndex = newChildIndex;
        }
    }

    private double Simulate(ref WarArena arena)
    {
        const int turnLimit = 100;

        for (var i = 0; i < turnLimit; i++)
        {
            if (arena.Snakes.Me.Dead) return double.NegativeInfinity;
            // if (arena.Snakes.LiveSnakesCount <= 1) return double.PositiveInfinity;

            var legalMovesMask = arena.Grid.GetLegalMoves(arena.Snakes.Me.Head);

            if (legalMovesMask == 0) return double.NegativeInfinity;
            
            var move = Heuristics.SelectRolloutMove(legalMovesMask, ref arena);
            
            arena.ApplySingleMove(move);
        }
        
        return Heuristics.Evaluate(ref arena);
    }
    
    private void Backpropagate(int startNodeIndex, double rawScore)
    {
        var normalizedResult = (double)Math.Sign(rawScore);

        var currentIndex = startNodeIndex;
        while (currentIndex != -1)
        {
            ref var currentNode = ref _nodePool[currentIndex];
            currentNode.UpdateStats(normalizedResult);
            currentIndex = currentNode.ParentIndex;
        }
    }
}