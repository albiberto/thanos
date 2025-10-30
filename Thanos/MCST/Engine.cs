using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
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
        _rootIndex = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int FindBestMove(in Request request, int previousMoveIndex)
    {
        if (previousMoveIndex > 0)
        {
            _rootIndex = FindNewRoot(previousMoveIndex, in request);

            if (_rootIndex > 0)
            {
                ref var newRootNode = ref _nodePool[_rootIndex];
                newRootNode.NewRoot();

                var rootArena = _slotPool.GetArena(_rootIndex);
                rootArena.InitializeFromRequest(in request);
            }
        }
        else
        {
            _rootIndex = 0;
        }

        if (_rootIndex == 0)
        {
            _rootIndex = 1;
            _worker.Reset(_rootIndex, request.Game.Ruleset.Settings);

            var hash = _slotPool.CalculateHash(_rootIndex, in request);
            ref var rootNode = ref _nodePool[_rootIndex];
            rootNode.PlacementRoot(hash);

            var rootArena = _slotPool.GetArena(_rootIndex);
            rootArena.InitializeFromRequest(in request);
        }

        RunIterations(request.Board.Area);

        return _nodePool.SelectMostVisitedChild(_rootIndex);
    }

    private int FindNewRoot(int myLastMoveNodeIndex, in Request request)
    {
        var currentHash = _slotPool.CalculateHash(1, in request);

        ref var myLastMoveNode = ref _nodePool[myLastMoveNodeIndex];
        if (myLastMoveNode.IsLeafNode) return 0;

        var childIndex = myLastMoveNode.FirstChildIndex;
        while (childIndex != -1)
        {
            ref var childNode = ref _nodePool[childIndex];
            
            #if DEBUG
            Console.WriteLine($"[Engine] Checking Child Index: {childIndex}");
            Console.WriteLine($"[Engine] Node: {JsonSerializer.Serialize(childNode)}");
            var arena = _slotPool.GetArena(childIndex);
            
            Console.WriteLine($"[Engine] Arena State for Child Index {childIndex}:");
            Console.WriteLine($"{arena.Snakes.ToGridString(11, 11)}");

            var system = arena.System;
            var totalSnakes = system.Count;
            
            for(var i = 0; i < totalSnakes; i++)
            {
                var snake = system[i];
                Console.WriteLine($"[Engine] Snake {i}");
                Console.WriteLine($"    Head: {snake.Head}");
                Console.WriteLine($"    Tail: {snake.Tail}");
                Console.WriteLine($"    Length: {snake.Length}");
                Console.WriteLine($"    Health: {snake.HP}");
                Console.WriteLine($"    IsDead: {snake.IsDead}");
                Console.WriteLine($"    Body Bitboard: {snake.Body.ToGridString(11, 11)}");
                Console.WriteLine($"    CircularBuffer: {string.Join(" -> ", snake._queue.Buffer.ToArray())}");
                Console.WriteLine($"    Head: {snake._queue.PeekHead}");
                Console.WriteLine($"    Tail: {snake._queue.PeekHead}");
                Console.WriteLine($"    HeadIndex: {snake._queue._state.HeadIndex}");
                Console.WriteLine($"    TailIndex: {snake._queue._state.TailIndex}");
                
            }
            #endif

            if (childNode.Hash == currentHash) return childIndex;

            childIndex = childNode.NextSiblingIndex;
        }

        return 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RunIterations(int area, int counter = 0)
    {
        var stopwatch = Stopwatch.StartNew();

        // while (stopwatch.ElapsedMilliseconds < 450)
        while (counter < 25)
        {
            _worker.RunIteration(area, _rootIndex);
            counter++;
        }
        
        stopwatch.Stop();
        
        Console.WriteLine($"[Engine] Completed {counter} iterations in {stopwatch.ElapsedMilliseconds} ms.");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Reset()
    {
        _rootIndex = 0;
        _worker.Reset(1);
    }
}