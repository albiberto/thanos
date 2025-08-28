using System.Diagnostics;
using System.Numerics;
using Thanos.Common;
using Thanos.Memory;
using Thanos.SourceGen;

namespace Thanos.MCST;

public class MonteCarloEngine(WarMemoryPool warPool, NodeMemoryPool nodePool)
{
    private readonly WarMemoryPool _warPool = warPool;
    private readonly NodeMemoryPool _nodePool = nodePool;

    public byte FindBestMove(in Request request)
    {
        var rootSlot = _warPool.GetNext();
        rootSlot.InitializeFromRequest(in request);

        var rootIndex = _nodePool.GetNextIndex();
        ref var rootNode = ref _nodePool[rootIndex];
        rootNode.Initialize(-1, Moves.None);

        var worker = new Worker(rootIndex, in rootSlot, _warPool, _nodePool);

        var counter = 0;
        var stopwatch = Stopwatch.StartNew();
        while (stopwatch.ElapsedMilliseconds < 450)
        {
            worker.RunIteration();
            counter++;
        }
        
        ref var finalRootNode = ref _nodePool[rootIndex];
        LogDebug(stopwatch, counter, finalRootNode);
        
        var bestChildIndex = finalRootNode.SelectBestChild(_nodePool, 0);
        
        if (bestChildIndex != -1) return _nodePool[bestChildIndex].MoveThatLedToThisNode;

        var legalMoves = rootSlot.General.Grid.GetLegalMoves(rootSlot.General.Snakes.Me.Head);
        return legalMoves != 0 ? (byte)(1 << BitOperations.TrailingZeroCount(legalMoves)) : Moves.Up;
    }

    /// <summary>
    /// Log per il DEBUG
    /// </summary>
    private void LogDebug(Stopwatch stopwatch, int counter, Node finalRootNode)
    {
        Console.WriteLine(">>> MCTS completato in {0} ms con {1} iterazioni", stopwatch.ElapsedMilliseconds, counter);
        Console.WriteLine(">>> Analisi finale dei figli:");
        foreach (var childIndex in finalRootNode.GetChildren(_nodePool))
        {
            ref var childNode = ref _nodePool[childIndex];
            if (childNode.Visits == 0) continue;
            var winRate = childNode.Wins / childNode.Visits;
            var moveName = childNode.MoveThatLedToThisNode switch { 1 => "Up", 2 => "Down", 4 => "Left", 8 => "Right", _ => "?" };
            Console.WriteLine($"  - {moveName,-5} | WinRate: {winRate:F3} | Visits: {childNode.Visits}");
        }
    }
}