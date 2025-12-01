using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Thanos.Common;
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
    public int FindBestMove(in Request request, int lastChosenIndex, long targetHash)
    {
        // 1. Tree Reuse Logic
        if (lastChosenIndex > 0)
        {
            _rootIndex = FindNewRoot(lastChosenIndex, targetHash);
            
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

        // 2. Full Reset Fallback
        if (_rootIndex <= 0)
        {
            _rootIndex = Constants.FirstRootNodeIndex; 
            _worker.Reset(_rootIndex, request.Game.Ruleset.Settings);

            var rootArena = _slotPool.GetArena(_rootIndex);
            rootArena.InitializeFromRequest(in request);
            
            ref var rootNode = ref _nodePool[_rootIndex];
            rootNode.PlacementRoot(targetHash);
        }

        RunIterations(request.Board.Area);

        // 3. Selection
        return _nodePool.SelectMostVisitedChild(_rootIndex);
    }

    private int FindNewRoot(int myLastMoveNodeIndex, long targetHash)
    {
        ref var myLastMoveNode = ref _nodePool[myLastMoveNodeIndex];
        return FindNodeWithHash(myLastMoveNode.FirstChildIndex, targetHash, 5);
    }
    
    private int FindNodeWithHash(int startIndex, long targetHash, int depthLimit)
    {
        if (startIndex <= 0 || depthLimit <= 0) return 0;

        var current = startIndex;
        
        var safetyCounter = 0;
        const int MaxSiblingsSearch = 10000; 

        while (current > 0 && safetyCounter++ < MaxSiblingsSearch)
        {
            ref var node = ref _nodePool[current];
            
            if (node.Hash == targetHash) return current;

            var foundInChild = FindNodeWithHash(node.FirstChildIndex, targetHash, depthLimit - 1);
            if (foundInChild != 0) return foundInChild;

            current = node.NextSiblingIndex;
        }
        
        return 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RunIterations(int area, int counter = 0)
    {
        const long maxTimeMs = 450;
        const long forcedMoveTimeMs = 50; 
        
        var stopwatch = Stopwatch.StartNew();
        
        if (_nodePool[_rootIndex].IsLeafNode) _worker.RunIteration(area, _rootIndex);
        
        ref var rootNode = ref _nodePool[_rootIndex];
        
        var childCount = 0;
        var childIdx = rootNode.FirstChildIndex;
        while(childIdx != -1)
        {
            childCount++;
            childIdx = _nodePool[childIdx].NextSiblingIndex;
        }

        var timeLimit = childCount <= 1 ? forcedMoveTimeMs : maxTimeMs;

        while (stopwatch.ElapsedMilliseconds < timeLimit)
        {
            if (rootNode.IsSolvedWin || rootNode.IsSolvedLoss) break;
            
            var remainingTime = timeLimit - stopwatch.ElapsedMilliseconds;

            var currentBatchSize = remainingTime switch
            {
                > 250 => 2048,
                > 150 => 1024,
                > 80 => 512,
                _ => 256
            };

            for(var i = 0; i < currentBatchSize; i++) 
            {
                _worker.RunIteration(area, _rootIndex);
            }
            counter += currentBatchSize;
        }

        stopwatch.Stop();
    }
    
    public unsafe void GetRootStats(List<RootMoveStat> outputBuffer)
    {
        outputBuffer.Clear();

        if (_rootIndex <= 0) return;

        ref var rootNode = ref _nodePool[_rootIndex];
        var childIndex = rootNode.FirstChildIndex;

        while (childIndex != -1)
        {
            ref var childNode = ref _nodePool[childIndex];
            
            if (childNode.Visits > 0 || childNode.IsSolvedWin)
            {
                var avgScore = childNode.Visits > 0 ? childNode.Rewards[0] / childNode.Visits : -1;
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

        // FIX COMPILAZIONE: Aggiunto parametro '0' (heroIndex)
        // Stiamo chiedendo le mosse legali per il serpente 0 (NOI).
        var legalMoves = arena.GetLegalMoves(me.Head, me.Tail, me.ElementBeforeTail, 0);

        if ((legalMoves & Moves.Up) != 0) return Moves.Up;
        if ((legalMoves & Moves.Down) != 0) return Moves.Down;
        if ((legalMoves & Moves.Left) != 0) return Moves.Left;
        if ((legalMoves & Moves.Right) != 0) return Moves.Right;

        return Moves.Up; 
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Reset()
    {
        _rootIndex = 0;
        _worker.Reset(1);
    }
}