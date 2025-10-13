using System.Diagnostics;
using System.Runtime.CompilerServices;
using Thanos.Memory;
using Thanos.SourceGen;
using Thanos.Extensions; 

namespace Thanos.MCST;

public class Engine
{
    private readonly NodeMemoryPool _nodePool;
    private readonly SlotMemoryPool _slotPool;
    private readonly Worker _worker;

    private int _rootIndex;

    public Engine(SlotMemoryPool slotPool, NodeMemoryPool nodePool)
    {
        _slotPool = slotPool;
        _nodePool = nodePool;
        _worker = new Worker(_slotPool, _nodePool);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int FindBestMove(in Request request)
    {
        // If _rootIndex is 0, it means we’re starting or a reset has occurred — we need to create a new tree.
        if (_rootIndex == 0)
        {
            #if DEBUG
                Console.WriteLine("[Engine.FindBestMove] INFO: Creating new MCTS tree from scratch.");
            #endif

            _rootIndex = 1;
            _worker.Reset(_rootIndex, request.Game.Ruleset.Settings);

            var hash = _slotPool.CalculateHash(_rootIndex, in request);
            ref var rootNode = ref _nodePool[_rootIndex];
            rootNode.PlacementRoot(hash);
        }
        else
        {
            // In this case, we’re reusing an existing tree. Just update the root node’s state.
            #if DEBUG
                Console.WriteLine($"[Engine.FindBestMove] INFO: Updating state of reused root node {_rootIndex}.");
            #endif
            
            var rootArena = _slotPool.GetArena(_rootIndex);
            rootArena.InitializeFromRequest(in request);
        }

        RunIterations();
        
        #if DEBUG
            return _nodePool.SelectMostVisitedChildWithLogging(_rootIndex);
        #else
            return _nodePool.SelectMostVisitedChild(_rootIndex);
        #endif
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RunIterations(int counter = 0)
    {
        var stopwatch = Stopwatch.StartNew();

        // while (stopwatch.ElapsedMilliseconds < 10000000000)
        while (counter < 1000)
        {
            _worker.RunIteration(_rootIndex);
            counter++;
        }
        
        stopwatch.Stop();
        
        #if DEBUG
            Console.WriteLine($"[Engine.FindBestMove.RunIterations] INFO: Iterations completed: {counter} in {stopwatch.ElapsedMilliseconds}ms.");
        #endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareNextTurn(int lastChosenIndex)
    {
        if (lastChosenIndex <= 0)
        {
            #if DEBUG
                Console.WriteLine("[Engine.PrepareNextTurn] INFO: No previous move to reuse. Resetting tree.");
            #endif
            
            Reset();
            return;
        }
        
        _rootIndex = lastChosenIndex;
        
        ref var node = ref _nodePool[_rootIndex];
        node.NewRoot();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Reset()
    {
        Console.WriteLine("[Engine.Reset] INFO: Resetting MCTS tree.");
        _rootIndex = 0;
        _worker.Reset(1);
    }
}