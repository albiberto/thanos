using System.Runtime.CompilerServices;
using Thanos.Common;
using Thanos.Memory;
using Thanos.SourceGen;
using Thanos.War;
using System.Text; // Aggiunto per StringBuilder

namespace Thanos.MCST;

public sealed class Worker(SlotMemoryPool slotPool, NodeMemoryPool nodePool)
{
    private const double EXPLORATION_PARAMETER = 1.41;

    private int _nextId = 1;
    private RulesetSettings _settings;
    
    private readonly NodeMemoryPool _nodePool = nodePool;
    private readonly SlotMemoryPool _slotPool = slotPool;
    
    private static readonly byte[] AllMoves = [Moves.Up, Moves.Down, Moves.Left, Moves.Right];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RunIteration(int rootIndex)
    {
        var leafIndex = Select(rootIndex);
        ref var leafNode = ref _nodePool[leafIndex];

        if (leafNode is { IsLeafNode: true, IsTerminal: false })
        {
            Expand(leafIndex, ref leafNode);
        }

        var outcome = Evaluate(leafIndex);

        Backpropagate(leafIndex, outcome);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int Select(int rootIndex)
    {
        var currentIndex = rootIndex;
        while (true)
        {
            ref var currentNode = ref _nodePool[currentIndex];
            
            if (currentNode.IsLeafNode || currentNode.IsTerminal)
            {
                return currentIndex;
            }

            var candidateIndex = SelectBestChild(ref currentNode);
            
            if (candidateIndex == -1)
            {
                currentNode.IsTerminal = true;
                return currentIndex;
            }

            currentIndex = candidateIndex;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int SelectBestChild(ref Node parentNode)
    {
        var bestScore = double.MinValue;
        var bestChildIndex = -1;
        
        var logParentVisits = Math.Log(parentNode.Visits);

        var childIndex = parentNode.FirstChildIndex;
        while (childIndex != -1)
        {
            ref var childNode = ref _nodePool[childIndex];

            if (childNode.Visits == 0)
            {
                return childIndex;
            }

            var exploitation = childNode.Wins / childNode.Visits;
            var exploration = EXPLORATION_PARAMETER * Math.Sqrt(logParentVisits / childNode.Visits);
            var uctScore = exploitation + exploration;
            
            if (uctScore > bestScore)
            {
                bestScore = uctScore;
                bestChildIndex = childIndex;
            }

            childIndex = childNode.NextSiblingIndex;
        }

        return bestChildIndex;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Expand(int parentIndex, ref Node parentNode)
    {
        var playerIndex = parentNode.PlayerIndex;
        
        var playerArena = _slotPool.GetArena(parentIndex);
        var playerSnake = playerArena.System[playerIndex];

        if (playerSnake.IsDead)
        {
            parentNode.IsTerminal = true;
            return;
        }
        
        var safeMoves = GetLegalMoves(in playerArena, in playerSnake, playerIndex);
        ExpandNode(parentIndex, safeMoves, ref parentNode, ref playerSnake, in playerArena);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte GetLegalMoves(in Arena arena, in WarSnake playerSnake, int playerSnakeIndex)
    {
        byte safeMoves = 0;
        
        var potentialMoves = arena.GetLegalMoves(playerSnake.Head);
    
        foreach (var move in AllMoves)
        {
            if ((potentialMoves & move) == 0) continue;

            var nextHead = arena.GetNewHeadPosition(playerSnake.Head, move);
            var isSquareSafe = true;

            for (var enemySnakeIndex = 0; enemySnakeIndex < arena.System.Count; enemySnakeIndex++)
            {
                if (enemySnakeIndex == playerSnakeIndex)
                {
                    continue;
                }
                
                var enemySnake = arena.System[enemySnakeIndex];
                if (enemySnake.IsDead || enemySnake.Length < playerSnake.Length || arena.ManhattanDistance(enemySnake.Head, nextHead) != 1) continue;
                
                isSquareSafe = false;
                break;
            }

            if (isSquareSafe)
            {
                safeMoves |= move;
            }
        }

        return safeMoves;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ExpandNode(int parentIndex, byte safeMoves, ref Node parentNode, ref WarSnake snake, in Arena parentArena)
    {
        var playerIndex = parentNode.PlayerIndex;
        var lastChildIndex = -1;

        foreach (var move in AllMoves)
        {
            if ((safeMoves & move) == 0) continue;

            var childIndex = ++_nextId;
            var childArena = _slotPool.GetArena(childIndex);
        
            childArena.CloneFrom(in parentArena);

            ApplySingleMove(ref childArena, ref snake, move);

            var nextPlayerIndex = GetNextPlayerIndex(in childArena, playerIndex);

            var hash = ZobristHasher.CalculateHash(in childArena);
            ref var childNode = ref _nodePool[childIndex];
            childNode.PlacementNew(parentIndex, move, hash, nextPlayerIndex);

            if (lastChildIndex == -1)
            {
                parentNode.FirstChildIndex = childIndex;
            }
            else
            {
                _nodePool[lastChildIndex].NextSiblingIndex = childIndex;
            }
            
            lastChildIndex = childIndex;
        }

        if (lastChildIndex == -1)
        {
            parentNode.IsTerminal = true;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte GetNextPlayerIndex(in Arena arena, int currentPlayerIndex)
    {
        var nextPlayerIndex = currentPlayerIndex;
        
        do
        {
            nextPlayerIndex = (nextPlayerIndex + 1) % arena.System.Count;
        }
        while (arena.System[nextPlayerIndex].IsDead && nextPlayerIndex != currentPlayerIndex);

        return (byte)nextPlayerIndex;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ApplySingleMove(ref Arena arena, ref WarSnake snake, byte move)
    {
        var newHead = arena.GetNewHeadPosition(snake.Head, move);
        
        var hasEaten = arena.Food.IsSet(newHead);
        var damage = arena.Hazards.IsSet(newHead) ? 10 : 1;
        var newTail = arena.CalculateNewTailPosition(snake, hasEaten);
        
        arena.Snakes.Xor(snake.Body);
        snake.UpdateAfterMove(newHead, newTail, hasEaten, damage);
        arena.Snakes.Or(snake.Body);
        
        if (hasEaten) arena.Food.Unset(newHead);
        
        arena.SimulateRandomFoodSpawn(_settings.FoodSpawnChance, _settings.MinimumFood);
    }
    
    private float Evaluate(int leafIndex)
    {
        var heuristics = _slotPool.GetHeuristics(leafIndex);
        var outcome = heuristics.Outcome();
        var score = outcome != 0.0f ? outcome : heuristics.Evaluate();
        
        #if DEBUG
            Console.WriteLine($"[Evaluate] Node {leafIndex} - Heuristic score: {score:F2}");
        #endif
        
        return score;
    }
    
    private void Backpropagate(int startNodeIndex, float outcome)
    {
        const float scalingFactor = 100.0f;
        var normalizedResult = MathF.Tanh(outcome / scalingFactor);
    
        var scoreToPropagate = normalizedResult;
        var currentIndex = startNodeIndex;

        while (currentIndex != -1)
        {
            ref var currentNode = ref _nodePool[currentIndex];
        
            var scoreForCurrentNode = currentNode.PlayerIndex == 0 ? scoreToPropagate : -scoreToPropagate;
            currentNode.UpdateStats(scoreForCurrentNode);
        
            scoreToPropagate *= -1;
        
            currentIndex = currentNode.ParentIndex;
        }
    }
    
    public void Reset(int startId) => _nextId = startId;
    public void Reset(int startId, RulesetSettings settings)
    {
        _nextId = startId;
        _settings = settings;
    }
}