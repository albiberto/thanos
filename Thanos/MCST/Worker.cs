using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Thanos.Abstract;
using Thanos.Common;
using Thanos.Memory;
using Thanos.SourceGen;
using Thanos.War;

namespace Thanos.MCST;

public sealed class Worker(ISlotMemoryPool slotPool, INodeMemoryPool nodeMemoryPool)
{
    private const double EXPLORATION_PARAMETER = 1.41;
    private const int CHANCE_NODE_VISIT_THRESHOLD = 50;

    private int _nextId = 1;
    private RulesetSettings _settings;

    private readonly INodeMemoryPool _nodeMemoryPool = nodeMemoryPool;
    private readonly ISlotMemoryPool _slotPool = slotPool;

    private static readonly byte[] AllMoves = [Moves.Up, Moves.Down, Moves.Left, Moves.Right];
    private readonly float[] _rewardsBuffer = new float[Constants.MaxSnakesCount];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RunIteration(int area, int rootIndex)
    {
        var leafIndex = Select(rootIndex);
        ref var leafNode = ref _nodeMemoryPool.Get(leafIndex);

        if (leafNode.IsLeafNode && leafNode is { IsTerminal: false, IsSolvedWin: false, IsSolvedLoss: false }) Expand(leafIndex, ref leafNode, area);

        var nodeToEvaluate = leafIndex;
        if (!leafNode.IsLeafNode) nodeToEvaluate = leafNode.FirstChildIndex;

        Evaluate(nodeToEvaluate, _rewardsBuffer);
        Backpropagate(nodeToEvaluate, _rewardsBuffer);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int Select(int rootIndex)
    {
        var currentIndex = rootIndex;
        while (true)
        {
            ref var currentNode = ref _nodeMemoryPool.Get(currentIndex);

            if (currentNode.IsLeafNode || currentNode.IsTerminal || currentNode.IsSolvedWin || currentNode.IsSolvedLoss)
                return currentIndex;

            if (currentNode.IsChanceNode)
            {
                var outcomeIndex = SelectChanceOutcome(ref currentNode);
                if (outcomeIndex == -1) return currentIndex;
                currentIndex = outcomeIndex;
                continue;
            }

            var bestChild = SelectBestChildMaxN(ref currentNode);
            if (bestChild == -1)
            {
                currentNode.MarkTerminal();
                return currentIndex;
            }

            currentIndex = bestChild;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe int SelectBestChildMaxN(ref Node parentNode)
    {
        var bestScore = double.MinValue;
        var bestChildIndex = -1;
        var logParentVisits = Math.Log(parentNode.Visits);
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

            if (childNode.Visits == 0) return childIndex;

            var exploitation = childNode.Rewards[playerIndex] / childNode.Visits;
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
    private int SelectChanceOutcome(ref Node parentNode)
    {
        var firstChild = parentNode.FirstChildIndex;
        if (firstChild == -1) return -1;

        ref var firstNode = ref _nodeMemoryPool.Get(firstChild);
        var secondChild = firstNode.NextSiblingIndex;

        if (secondChild == -1) return firstChild;

        var pickSpawn = Random.Shared.NextDouble() < _settings.FoodSpawnChance / 100.0;
        return pickSpawn ? secondChild : firstChild;
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

        if (snake.IsDead)
        {
            parentNode.MarkTerminal();
            return;
        }

        var legalMoves = arena.GetLegalMoves(snake.Head, snake.Tail, snake.ElementBeforeTail, playerIndex);

        if (legalMoves == 0)
        {
            parentNode.MarkTerminal();
            return;
        }

        byte prunedMoves = 0;
        var safeMoveCount = 0;
        foreach (var move in AllMoves)
        {
            if ((legalMoves & move) == 0) continue;
            if (!IsMoveRisky(in arena, snake.Head, move))
            {
                prunedMoves |= move;
                safeMoveCount++;
            }
        }

        var movesToExpand = safeMoveCount > 0 ? prunedMoves : legalMoves;

        var nextPlayerIndex = GetNextPlayerIndex(in arena, playerIndex);
        var isNextChance = nextPlayerIndex == Constants.EnvironmentPlayerIndex;
        var actualNextPlayer = isNextChance ? (byte)Constants.EnvironmentPlayerIndex : (byte)nextPlayerIndex;

        var lastChildIndex = -1;
        foreach (var move in AllMoves)
        {
            if ((movesToExpand & move) == 0) continue;

            var childIndex = ++_nextId;
            var childArena = _slotPool.GetArena(childIndex);

            childArena.CloneFrom(in arena);

            var snakeToMove = childArena.System[playerIndex];
            ApplySingleMove(in childArena, ref snakeToMove, move, area);

            var hash = ZobristHasher.CalculateHash(in childArena);

            ref var childNode = ref _nodeMemoryPool.Get(childIndex);
            childNode.PlacementNew(parentIndex, move, hash, actualNextPlayer, isNextChance);

            if (lastChildIndex == -1) parentNode.FirstChildIndex = childIndex;
            else _nodeMemoryPool.Get(lastChildIndex).NextSiblingIndex = childIndex;

            lastChildIndex = childIndex;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsMoveRisky(in Arena arena, ushort currentHead, byte move) => false;

    private void ExpandChanceNode(int parentIndex, ref Node parentNode, int area)
    {
        CreateEnvironmentChild(parentIndex, area, false);
        if (parentNode.Visits > CHANCE_NODE_VISIT_THRESHOLD)
            CreateEnvironmentChild(parentIndex, area, true);
    }

    private void CreateEnvironmentChild(int parentIndex, int area, bool spawnFood)
    {
        var childIndex = ++_nextId;
        var parentArena = _slotPool.GetArena(parentIndex);
        var childArena = _slotPool.GetArena(childIndex);
        childArena.CloneFrom(in parentArena);

        if (spawnFood) childArena.SimulateRandomFoodSpawn(_settings.FoodSpawnChance, _settings.MinimumFood, area);

        var hash = ZobristHasher.CalculateHash(in childArena);

        ref var childNode = ref _nodeMemoryPool.Get(childIndex);
        ref var parentNode = ref _nodeMemoryPool.Get(parentIndex);

        var firstAlive = GetFirstAlivePlayerIndex(in childArena);
        var isNextChance = firstAlive == Constants.EnvironmentPlayerIndex;
        var nextPlayer = (byte)firstAlive;

        childNode.PlacementNew(parentIndex, Moves.None, hash, nextPlayer, isNextChance);
        if (isNextChance) childNode.MarkTerminal();

        if (parentNode.FirstChildIndex == -1)
        {
            parentNode.FirstChildIndex = childIndex;
        }
        else
        {
            var sibling = parentNode.FirstChildIndex;
            while (_nodeMemoryPool.Get(sibling).NextSiblingIndex != -1) sibling = _nodeMemoryPool.Get(sibling).NextSiblingIndex;
            _nodeMemoryPool.Get(sibling).NextSiblingIndex = childIndex;
        }
    }

    private static int GetNextPlayerIndex(in Arena arena, int currentPlayerIndex)
    {
        var next = currentPlayerIndex + 1;
        while (next < arena.System.Count && arena.System[next].IsDead) next++;
        if (next >= arena.System.Count) return Constants.EnvironmentPlayerIndex;
        return next;
    }

    private static int GetFirstAlivePlayerIndex(in Arena arena)
    {
        var next = 0;
        while (next < arena.System.Count && arena.System[next].IsDead) next++;
        if (next >= arena.System.Count) return Constants.EnvironmentPlayerIndex;
        return next;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ApplySingleMove(in Arena arena, ref WarSnake snake, byte move, int area)
    {
        var newHead = arena.GetNewHeadPosition(snake.Head, move);

        arena.Snakes.Xor(snake.Body);

        if (arena.Snakes.IsSet(newHead))
            for (var i = 0; i < arena.System.Count; i++)
            {
                var enemy = arena.System[i];
                if (enemy.IsOnBody(newHead))
                {
                    if (enemy.Head == newHead)
                    {
                        if (snake.Length <= enemy.Length) snake.Kill();
                        if (snake.Length >= enemy.Length)
                        {
                            arena.System[i].Kill();
                            arena.Snakes.Xor(arena.System[i].Body);
                        }
                    }
                    else
                    {
                        snake.Kill();
                    }
                }
            }

        if (snake.IsDead) return;

        var hasEaten = arena.Food.IsSet(newHead);
        var damage = arena.Hazards.IsSet(newHead) ? _settings.HazardDamagePerTurn : 1;

        snake.UpdateAfterMove(newHead, hasEaten, damage);
        arena.Snakes.Or(snake.Body);

        if (hasEaten) arena.Food.Unset(newHead);
    }

    private void Evaluate(int nodeIndex, float[] rewardsBuffer)
    {
        var heuristics = _slotPool.GetHeuristics(nodeIndex);
        var arena = _slotPool.GetArena(nodeIndex);

        // MODIFICA: Determina se il turno è completo
        // Se il prossimo a muovere è Environment o Player 0 (noi, inizio nuovo turno), 
        // significa che tutti gli avversari hanno mosso nel turno precedente.
        ref var node = ref _nodeMemoryPool.Get(nodeIndex);
        var isPhaseComplete = node.PlayerIndex is Constants.EnvironmentPlayerIndex or 0;

        Array.Clear(rewardsBuffer);

        for (var i = 0; i < arena.System.Count; i++)
        {
            var outcome = heuristics.Outcome(i);
            if (outcome != 0.0f) rewardsBuffer[i] = outcome;
        }

        Span<float> rawScores = stackalloc float[arena.System.Count];

        // Passiamo il flag isPhaseComplete all'euristica
        heuristics.EvaluateAll(rawScores, isPhaseComplete);

        for (var i = 0; i < arena.System.Count; i++)
        {
            if (rewardsBuffer[i] != 0.0f) continue;
            if (arena.System[i].IsDead)
            {
                rewardsBuffer[i] = -1.0f;
                continue;
            }

            rewardsBuffer[i] = MathF.Tanh(rawScores[i] / 150.0f);
        }
    }

    private unsafe void Backpropagate(int startNodeIndex, float[] rewards)
    {
        var currentIndex = startNodeIndex;
        while (currentIndex != -1)
        {
            ref var currentNode = ref _nodeMemoryPool.Get(currentIndex);
            currentNode.UpdateStats(rewards);
            if (currentNode.ParentIndex != -1) PropagateSolverFlags(currentIndex, currentNode.ParentIndex);
            currentIndex = currentNode.ParentIndex;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void PropagateSolverFlags(int childIndex, int parentIndex)
    {
        ref var childNode = ref _nodeMemoryPool.Get(childIndex);
        if (childNode.IsSolvedLoss)
        {
            ref var parentNode = ref _nodeMemoryPool.Get(parentIndex);
            if (parentNode.IsSolvedLoss || parentNode.IsSolvedWin) return;
            if (parentNode.IsChanceNode) return;

            var allChildrenLost = true;
            var currentSibling = parentNode.FirstChildIndex;
            while (currentSibling != -1)
            {
                ref var siblingNode = ref _nodeMemoryPool.Get(currentSibling);
                if (!siblingNode.IsSolvedLoss)
                {
                    allChildrenLost = false;
                    break;
                }

                currentSibling = siblingNode.NextSiblingIndex;
            }

            if (allChildrenLost) parentNode.MarkSolvedLoss();
        }
    }

    public void Reset(int startId) => _nextId = startId;

    public void Reset(int startId, RulesetSettings settings)
    {
        _nextId = startId;
        _settings = settings;
    }
}