using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
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

    private int _rootIndex = Constants.FirstRootNodeIndex;

    public Engine(SlotMemoryPool slotPool, NodeMemoryPool nodePool)
    {
        _slotPool = slotPool;
        _nodePool = nodePool;
        _worker = new Worker(_slotPool, _nodePool);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int FindBestMove(in Request request, int lastChosenIndex)
    {
        if (lastChosenIndex > 0)
        {
            _rootIndex = FindNewRoot(lastChosenIndex, in request);

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
            _rootIndex = 1;
        }

        if (_rootIndex == 0)
        {
            _rootIndex = 1;
            _worker.Reset(_rootIndex, request.Game.Ruleset.Settings);

            var hash = _slotPool.CalculateRequestHash(_rootIndex, in request);
            ref var rootNode = ref _nodePool[_rootIndex];
            rootNode.PlacementRoot(hash);

            var rootArena = _slotPool.GetArena(_rootIndex);
            rootArena.InitializeFromRequest(in request);
        }

        RunIterations(request.Board.Area);

#if DEBUG
        LogFullTreeState();
#endif

        return _nodePool.SelectMostVisitedChild(_rootIndex);
    }

    private int FindNewRoot(int myLastMoveNodeIndex, in Request request)
    {
        var currentHash = _slotPool.CalculateRequestHash(1, in request);

        ref var myLastMoveNode = ref _nodePool[myLastMoveNodeIndex];
        if (myLastMoveNode.IsLeafNode) return 0;

        var childIndex = myLastMoveNode.FirstChildIndex;
        while (childIndex != -1)
        {
            ref var childNode = ref _nodePool[childIndex];

#if DEBUG
            Console.WriteLine($"[Engine] Comparing Child Node Hash: {childNode.Hash} with Current Hash: {currentHash}");
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

        #if !DEBUG
            while (stopwatch.ElapsedMilliseconds < 450)
        #else
            while (counter < 50)
        #endif
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

    public void LogFullTreeState()
    {
        var sb = new StringBuilder();

        sb.AppendLine($"[Engine] --- INIZIO LOG COMPLETO ALBERO (Partendo da root: {_rootIndex}) ---");
        LogNodeRecursive(_rootIndex, sb, 0);
        sb.AppendLine("[Engine] --- FINE LOG COMPLETO ALBERO ---");

        Console.WriteLine(sb.ToString());
    }

    private void LogNodeRecursive(int nodeIndex, StringBuilder sb, int depth)
    {
        if (nodeIndex == -1) return;

        var indent = new string(' ', depth * 4);

        sb.AppendLine($"{indent}--- [Stato Nodo {nodeIndex}] ---");
        Log(nodeIndex, sb, indent);
        sb.AppendLine($"{indent}-------------------------");

        ref var node = ref _nodePool[nodeIndex];

        var childIndex = node.FirstChildIndex;
        while (childIndex != -1)
        {
            LogNodeRecursive(childIndex, sb, depth + 1);

            ref var childNode = ref _nodePool[childIndex];
            childIndex = childNode.NextSiblingIndex;
        }
    }

    private void Log(int childIndex, StringBuilder sb, string indent)
    {
        ref var childNode = ref _nodePool[childIndex];

        sb.AppendLine($"{indent}[Engine] Checking Child Index: {childIndex}");
        sb.AppendLine($"{indent}[Engine] Node: {JsonSerializer.Serialize(childNode)}");
        var arena = _slotPool.GetArena(childIndex);

        sb.AppendLine($"{indent}[Engine] Arena State for Child Index {childIndex}:");
        var grid = arena.Snakes.ToGridString(11, 11);
        sb.AppendLine($"{indent}{grid.Replace("\n", $"\n{indent}")}");

        var system = arena.System;
        var totalSnakes = system.Count;

        for (var i = 0; i < totalSnakes; i++)
        {
            var snake = system[i];

            var snakeIndent = indent + "    ";

            sb.AppendLine($"{snakeIndent}Snake {i}");
            sb.AppendLine($"{snakeIndent}    Head: {snake.Head}");
            sb.AppendLine($"{snakeIndent}    Tail: {snake.Tail}");
            sb.AppendLine($"{snakeIndent}    Length: {snake.Length}");
            sb.AppendLine($"{snakeIndent}    Health: {snake.HP}");
            sb.AppendLine($"{snakeIndent}    IsDead: {snake.IsDead}");
            sb.AppendLine($"{snakeIndent}    Body Bitboard:");

            var snakeGrid = snake.Body.ToGridString(11, 11);
            sb.AppendLine($"{snakeIndent}    {snakeGrid.Replace("\n", $"\n{snakeIndent}    ")}");

            sb.AppendLine($"{snakeIndent}    CircularBuffer: {string.Join(" -> ", snake._queue.Buffer.ToArray())}");
            sb.AppendLine($"{snakeIndent}    Head: {snake._queue.PeekHead}");
            sb.AppendLine($"{snakeIndent}    Tail: {snake._queue.PeekTail}");
            sb.AppendLine($"{snakeIndent}    HeadIndex: {snake._queue._state.HeadIndex}");
            sb.AppendLine($"{snakeIndent}    TailIndex: {snake._queue._state.TailIndex}");
        }
    }
}