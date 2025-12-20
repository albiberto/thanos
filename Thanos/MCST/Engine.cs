using System.Diagnostics;
using System.Runtime.CompilerServices;
using Thanos.Abstract;
using Thanos.Common;
using Thanos.Memory;
using Thanos.SourceGen;

namespace Thanos.MCST;

public sealed class Engine
{
    private readonly ISlotMemoryPool _slotPool;
    private readonly INodeMemoryPool _nodePool;
    private readonly Worker _worker;

    private int _rootIndex = -1; 
    private string[] _sortedSnakeIds = [];

    public Engine(ISlotMemoryPool slotPool, INodeMemoryPool nodePool)
    {
        _slotPool = slotPool;
        _nodePool = nodePool;
        _worker = new Worker(_slotPool, _nodePool);
    }

    public void InitializeGame(string[] sortedSnakeIds)
    {
        _sortedSnakeIds = sortedSnakeIds;
        Reset();
    }

    // Metodo di reset leggero chiamato dal Cluster
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Reset()
    {
        _rootIndex = -1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int FindBestMove(in Request request, int lastChosenIndex, long targetHash)
    {
        var treeReused = false;

        // FASE 1: Tree Reuse
        if (_rootIndex != -1 && lastChosenIndex > 0)
        {
            var potentialRoot = FindNewRoot(lastChosenIndex, targetHash);
            if (potentialRoot > 0)
            {
                _rootIndex = potentialRoot;
                ref var newRootNode = ref _nodePool.Get(_rootIndex);
                newRootNode.NewRoot(); 

                var rootArena = _slotPool.GetArena(_rootIndex);
                rootArena.InitializeFromRequest(in request, _sortedSnakeIds);
                treeReused = true;
            }
        }

        // FASE 2: Full Reset
        if (!treeReused)
        {
            _nodePool.Reset();
            _slotPool.Reset();

            _rootIndex = _nodePool.Allocate();
            var slotIndex = _slotPool.Allocate();

            if (_rootIndex == -1 || slotIndex == -1) 
                throw new InvalidOperationException("Pools exhausted.");
            
            Debug.Assert(_rootIndex == slotIndex);

            var rootArena = _slotPool.GetArena(_rootIndex);
            rootArena.InitializeFromRequest(in request, _sortedSnakeIds);

            ref var rootNode = ref _nodePool.Get(_rootIndex);
            rootNode.PlacementRoot(targetHash); 
        }

        // FASE 3: MCTS
        RunIterations(request.Board.Area);

        // FASE 4: Selection
        return SelectBestChildMove(_rootIndex);
    }

    // ... (FindNewRoot, FindNodeWithHash, RunIterations, SelectBestChildMove, GetRootStats, GetFallbackMove) ...
    // Assumo che questi metodi siano presenti come definiti precedentemente.

    private int FindNewRoot(int myLastMoveNodeIndex, long targetHash)
    {
        ref var myLastMoveNode = ref _nodePool.Get(myLastMoveNodeIndex);
        return FindNodeWithHash(myLastMoveNode.FirstChildIndex, targetHash, 5);
    }

    private int FindNodeWithHash(int startIndex, long targetHash, int depthLimit)
    {
        if (startIndex <= 0 || depthLimit <= 0) return 0;
        var current = startIndex;
        var safetyCounter = 0;
        const int MaxSiblingsSearch = 5000;

        while (current > 0 && safetyCounter++ < MaxSiblingsSearch)
        {
            ref var node = ref _nodePool.Get(current);
            if (node.Hash == targetHash) return current;
            var foundInChild = FindNodeWithHash(node.FirstChildIndex, targetHash, depthLimit - 1);
            if (foundInChild != 0) return foundInChild;
            current = node.NextSiblingIndex;
        }
        return 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RunIterations(int area)
    {
        const long maxTimeMs = 450;
        const long forcedMoveTimeMs = 50; 
        var stopwatch = Stopwatch.StartNew();
        ref var rootNode = ref _nodePool.Get(_rootIndex);

        if (rootNode.IsLeafNode) _worker.RunIteration(area, _rootIndex);

        var childCount = CountChildren(_rootIndex);
        var timeLimit = childCount <= 1 ? forcedMoveTimeMs : maxTimeMs;

        while (stopwatch.ElapsedMilliseconds < timeLimit)
        {
            if (rootNode.IsSolvedWin || rootNode.IsSolvedLoss) break;
            var remainingTime = timeLimit - stopwatch.ElapsedMilliseconds;
            var currentBatchSize = remainingTime switch
            {
                > 250 => 1500, > 100 => 500, > 50 => 100, _ => 10
            };

            for (var i = 0; i < currentBatchSize; i++) _worker.RunIteration(area, _rootIndex);
        }
        stopwatch.Stop();
    }

    private unsafe int SelectBestChildMove(int rootIndex)
    {
        ref var rootNode = ref _nodePool.Get(rootIndex);
        var bestMove = Moves.Up; 
        var maxVisits = -1;
        var maxScore = float.NegativeInfinity;

        var childIndex = rootNode.FirstChildIndex;
        while (childIndex != -1)
        {
            ref var child = ref _nodePool.Get(childIndex);
            
            if (child.Visits > maxVisits)
            {
                maxVisits = child.Visits;
                maxScore = child.Rewards[0];
                bestMove = child.Move;
            }
            else if (child.Visits == maxVisits)
            {
                if (child.Rewards[0] > maxScore)
                {
                    maxScore = child.Rewards[0];
                    bestMove = child.Move;
                }
            }
            childIndex = child.NextSiblingIndex;
        }
        return bestMove;
    }
    
    private int CountChildren(int nodeIndex)
    {
        var count = 0;
        ref var node = ref _nodePool.Get(nodeIndex);
        var child = node.FirstChildIndex;
        while (child != -1) { count++; child = _nodePool.Get(child).NextSiblingIndex; }
        return count;
    }

    public unsafe void GetRootStats(List<RootMoveStat> outputBuffer)
    {
        outputBuffer.Clear();
        if (_rootIndex <= 0) return;

        ref var rootNode = ref _nodePool.Get(_rootIndex);
        var childIndex = rootNode.FirstChildIndex;

        while (childIndex != -1)
        {
            ref var childNode = ref _nodePool.Get(childIndex);
            if (childNode.Visits > 0)
            {
                var avgScore = childNode.Rewards[0] / childNode.Visits;
                outputBuffer.Add(new RootMoveStat(childNode.Move, childNode.Visits, avgScore));
            }
            childIndex = childNode.NextSiblingIndex;
        }
    }

    public byte GetFallbackMove()
    {
        if (_rootIndex <= 0) return Moves.Up;
        var arena = _slotPool.GetArena(_rootIndex);
        var me = arena.System[0];
        var legalMoves = arena.GetLegalMoves(me.Head, me.Tail, me.ElementBeforeTail, 0);

        if ((legalMoves & Moves.Up) != 0) return Moves.Up;
        if ((legalMoves & Moves.Down) != 0) return Moves.Down;
        if ((legalMoves & Moves.Left) != 0) return Moves.Left;
        if ((legalMoves & Moves.Right) != 0) return Moves.Right;
        return Moves.Up;
    }
}