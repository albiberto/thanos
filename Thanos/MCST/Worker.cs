using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Thanos.Abstract;
using Thanos.Common;
using Thanos.SourceGen;
using Thanos.War;

namespace Thanos.MCST;

public sealed class Worker(ISlotMemoryPool slotPool, INodeMemoryPool nodeMemoryPool)
{
    private const float EXPLORATION_PARAMETER = 1.41f; 
    private const int CHANCE_NODE_VISIT_THRESHOLD = 50;

    private RulesetSettings _settings;
    private readonly INodeMemoryPool _nodeMemoryPool = nodeMemoryPool;
    private readonly ISlotMemoryPool _slotPool = slotPool;
    
    private static readonly byte[] AllMoves = [Moves.Up, Moves.Down, Moves.Left, Moves.Right];
    private readonly float[] _rewardsBuffer = new float[Constants.MaxSnakesCount];
    
    // Buffer per shuffle mosse nel playout
    private readonly byte[] _moveBuffer = new byte[4]; 

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RunIteration(int area, int rootIndex)
    {
        var leafIndex = Select(rootIndex);
        if (leafIndex == -1) return;

        ref var leafNode = ref _nodeMemoryPool.Get(leafIndex);

        if (leafNode.IsLeafNode && !leafNode.IsTerminal && !leafNode.IsSolvedWin && !leafNode.IsSolvedLoss) 
        {
            Expand(leafIndex, ref leafNode, area);
        }

        var nodeToEvaluate = leafIndex;
        if (!leafNode.IsLeafNode) nodeToEvaluate = leafNode.FirstChildIndex;

        RunPlayout(area, nodeToEvaluate);
        
        Evaluate(nodeToEvaluate, _rewardsBuffer);
        Backpropagate(nodeToEvaluate, _rewardsBuffer);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int Select(int rootIndex)
    {
        var currentIndex = rootIndex;
        var depth = 0; 
        const int maxDepth = 250;

        while (depth++ < maxDepth)
        {
            ref var currentNode = ref _nodeMemoryPool.Get(currentIndex);

            if (currentNode.IsLeafNode || currentNode.IsTerminal || currentNode.IsSolvedWin || currentNode.IsSolvedLoss)
                return currentIndex;

            if (currentNode.IsChanceNode)
            {
                var outcome = SelectChanceOutcome(ref currentNode);
                if (outcome == -1) return currentIndex;
                currentIndex = outcome;
            }
            else
            {
                var bestChild = SelectBestChildUCT(ref currentNode);
                if (bestChild == -1)
                {
                    currentNode.MarkTerminal();
                    return currentIndex;
                }
                currentIndex = bestChild;
            }
        }
        return currentIndex;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe int SelectBestChildUCT(ref Node parentNode)
    {
        var bestScore = float.MinValue;
        var bestChildIndex = -1;
        var logParentVisits = parentNode.LogVisits; 
        var playerIndex = parentNode.PlayerIndex;

        var childIndex = parentNode.FirstChildIndex;
        while (childIndex != -1)
        {
            ref var childNode = ref _nodeMemoryPool.Get(childIndex);

            if (childNode.IsSolvedWin) return childIndex;
            if (childNode.IsSolvedLoss) 
            {
                childIndex = childNode.NextSiblingIndex;
                continue;
            }

            float uctScore;
            if (childNode.Visits == 0)
            {
                uctScore = 10000.0f + (childNode.Hash & 0xFF); 
            }
            else
            {
                var exploit = childNode.Rewards[playerIndex] / childNode.Visits;
                var explore = EXPLORATION_PARAMETER * MathF.Sqrt(logParentVisits / childNode.Visits);
                uctScore = exploit + explore;
            }

            if (uctScore > bestScore)
            {
                bestScore = uctScore;
                bestChildIndex = childIndex;
            }

            childIndex = childNode.NextSiblingIndex;
        }

        return bestChildIndex != -1 ? bestChildIndex : parentNode.FirstChildIndex;
    }

    private void RunPlayout(int area, int nodeIndex)
    {
        var arena = _slotPool.GetArena(nodeIndex);
        int depth = 0;
        const int PLAYOUT_DEPTH = 10; // Playout leggermente più profondo

        while (depth < PLAYOUT_DEPTH)
        {
            bool anyoneMoved = false;
            for (int i = 0; i < arena.System.Count; i++)
            {
                var snake = arena.System[i]; 
                if (snake.IsDead) continue;

                var moves = arena.GetLegalMoves(snake.Head, snake.Tail, snake.ElementBeforeTail, i);
                if (moves == 0) { snake.Kill(); continue; }

                // --- SMART RANDOM PLAYOUT ---
                int movesCount = 0;
                if ((moves & Moves.Up) != 0) _moveBuffer[movesCount++] = Moves.Up;
                if ((moves & Moves.Down) != 0) _moveBuffer[movesCount++] = Moves.Down;
                if ((moves & Moves.Left) != 0) _moveBuffer[movesCount++] = Moves.Left;
                if ((moves & Moves.Right) != 0) _moveBuffer[movesCount++] = Moves.Right;
                
                // Scegli a caso tra le mosse valide (rimuove il bias UP/LEFT)
                byte chosenMove = _moveBuffer[Random.Shared.Next(movesCount)];
                
                ApplySingleMove(in arena, ref snake, chosenMove, area);
                anyoneMoved = true;
            }
            if (!anyoneMoved) break;
            depth++;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int SelectChanceOutcome(ref Node parentNode)
    {
        var child = parentNode.FirstChildIndex;
        return child == -1 ? -1 : child;
    }

    private void Expand(int parentIndex, ref Node parentNode, int area)
    {
        if (parentNode.IsChanceNode) ExpandChanceNode(parentIndex, ref parentNode, area);
        else ExpandPlayerNode(parentIndex, ref parentNode, area);
    }

    private void ExpandPlayerNode(int parentIndex, ref Node parentNode, int area)
    {
        var playerIndex = parentNode.PlayerIndex;
        var arena = _slotPool.GetArena(parentIndex);
        var snake = arena.System[playerIndex];
        var legalMoves = arena.GetLegalMoves(snake.Head, snake.Tail, snake.ElementBeforeTail, playerIndex);
        
        if(legalMoves == 0) { parentNode.MarkTerminal(); return; }

        var nextPlayer = (byte)((playerIndex + 1) % arena.System.Count);
        
        foreach(var move in AllMoves)
        {
            if((legalMoves & move) == 0) continue;
            
            int childIdx = _nodeMemoryPool.Allocate();
            int slotIdx = _slotPool.Allocate();
            if(childIdx == -1 || slotIdx == -1) return;

            var childArena = _slotPool.GetArena(childIdx);
            childArena.CloneFrom(in arena);
            
            var childSnake = childArena.System[playerIndex];
            ApplySingleMove(in childArena, ref childSnake, move, area);
            
            ref var childNode = ref _nodeMemoryPool.Get(childIdx);
            childNode.PlacementNew(parentIndex, move, 0, nextPlayer, false);
            
            childNode.NextSiblingIndex = parentNode.FirstChildIndex;
            parentNode.FirstChildIndex = childIdx;
        }
    }

    private void ExpandChanceNode(int parentIndex, ref Node parentNode, int area) { }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ApplySingleMove(in Arena arena, ref WarSnake snake, byte move, int area)
    {
        var newHead = arena.GetNewHeadPosition(snake.Head, move);
        snake.UpdateAfterMove(newHead, false, 0);
    }

    private void Evaluate(int nodeIndex, float[] rewards)
    {
        var heuristics = _slotPool.GetHeuristics(nodeIndex);
        heuristics.EvaluateAll(new Span<float>(rewards), false);
    }

    private void Backpropagate(int nodeIndex, float[] rewards)
    {
        while(nodeIndex != -1)
        {
            ref var node = ref _nodeMemoryPool.Get(nodeIndex);
            node.UpdateStats(rewards);
            nodeIndex = node.ParentIndex;
        }
    }
    
    public void Reset(RulesetSettings s) => _settings = s;
}