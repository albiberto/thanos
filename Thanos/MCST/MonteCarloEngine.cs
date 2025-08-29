using System.Diagnostics;
using System.Numerics;
using Thanos.Common;
using Thanos.Memory;
using Thanos.SourceGen;

namespace Thanos.MCST;

public class MonteCarloEngine
{
    private readonly WarMemoryPool _warPool;
    private readonly NodeMemoryPool _nodePool;
    private readonly Worker _worker;
    
    public MonteCarloEngine(WarMemoryPool warPool, NodeMemoryPool nodePool)
    {
        _warPool = warPool;
        _nodePool = nodePool;
        _worker = new Worker(_warPool, _nodePool);
    }

    public byte FindBestMove(in Request request)
    {
        var slot = _warPool.GetNext();
        slot.InitializeFromRequest(in request);

        var rootIndex = _nodePool.GetNextIndex();
        ref var root = ref _nodePool[rootIndex];
        root.Initialize(-1, Moves.None);

        var counter = 0;
        var stopwatch = Stopwatch.StartNew();
        while (stopwatch.ElapsedMilliseconds < 450)
        {
            // 4. Usiamo il nostro worker riutilizzabile, passando i dati del turno
            _worker.RunIteration(rootIndex, in slot);
            counter++;
        }
        
        ref var finalRootNode = ref _nodePool[rootIndex];
        
        var bestChildIndex = finalRootNode.SelectBestChild(_nodePool, 0);
        
        if (bestChildIndex != -1) return _nodePool[bestChildIndex].MoveThatLedToThisNode;

        var initialArena = slot.Arena; 
        var legalMoves = initialArena.GetLegalMoves();
    
        Console.WriteLine($"[MCST] No best move found after {counter} iterations in {stopwatch.ElapsedMilliseconds}ms. Legal moves: {Convert.ToString(legalMoves, 2).PadLeft(4, '0')}");
        
        return legalMoves != 0 ? (byte)(1 << BitOperations.TrailingZeroCount(legalMoves)) : Moves.Up;
    }
}