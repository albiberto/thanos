using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Thanos.Common;
using Thanos.Memory;
using Thanos.PreWarm;
using Thanos.SourceGen;
using Thanos.War;

namespace Thanos.MCST;

public sealed class Worker(SlotMemoryPool slotPool, NodeMemoryPool nodePool)
{
    private const double EXPLORATION_PARAMETER = 1.41;
    private const int CHANCE_NODE_VISIT_THRESHOLD = 50;

    private int _nextId = 1;
    private RulesetSettings _settings;

    private readonly NodeMemoryPool _nodePool = nodePool;
    private readonly SlotMemoryPool _slotPool = slotPool;

    private static readonly byte[] AllMoves = [Moves.Up, Moves.Down, Moves.Left, Moves.Right];
    private readonly float[] _rewardsBuffer = new float[Constants.MaxSnakesCount];

    // ... [Il metodo RunIteration e Select rimangono uguali] ...
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RunIteration(int area, int rootIndex)
    {
        var leafIndex = Select(rootIndex);
        ref var leafNode = ref _nodePool[leafIndex];

        if (leafNode.IsLeafNode && !leafNode.IsTerminal && !leafNode.IsSolvedWin && !leafNode.IsSolvedLoss)
        {
            Expand(leafIndex, ref leafNode, area);
        }

        var nodeToEvaluate = leafIndex;
        if (!leafNode.IsLeafNode) nodeToEvaluate = leafNode.FirstChildIndex;

        Evaluate(nodeToEvaluate, _rewardsBuffer);
        Backpropagate(nodeToEvaluate, _rewardsBuffer);
    }

    // ... [Select, SelectBestChildMaxN, SelectChanceOutcome rimangono uguali] ...
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int Select(int rootIndex)
    {
        var currentIndex = rootIndex;
        while (true)
        {
            ref var currentNode = ref _nodePool[currentIndex];

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
            ref var childNode = ref _nodePool[childIndex];

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
        
        ref var firstNode = ref _nodePool[firstChild];
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

        // LOG DEBUG: Stato iniziale espansione
        Console.WriteLine($"[DEBUG-EXPAND] Node {parentIndex}: Player {playerIndex} expanding. Head: {snake.Head}, Dead: {snake.IsDead}");

        if (snake.IsDead)
        {
            Console.WriteLine($"[DEBUG-EXPAND] Player {playerIndex} is DEAD. Marking Terminal.");
            parentNode.MarkTerminal();
            parentNode.MarkSolvedLoss();
            return;
        }

        var legalMoves = arena.GetLegalMoves(snake.Head, snake.Tail, snake.ElementBeforeTail);
        
        if (legalMoves == 0)
        {
            Console.WriteLine($"[DEBUG-EXPAND] Player {playerIndex} has NO LEGAL MOVES (Wall/Body). Marking Terminal.");
            parentNode.MarkTerminal();
            parentNode.MarkSolvedLoss();
            return;
        }

        // --- PRUNING LOGIC ---
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
        var movesToExpand = (safeMoveCount > 0) ? prunedMoves : legalMoves;
        // ---------------------

        // LOG DEBUG: Mosse
        Console.WriteLine($"[DEBUG-EXPAND] P{playerIndex} Moves - Legal: {legalMoves}, Pruned: {movesToExpand}");

        var nextPlayerIndex = GetNextPlayerIndex(in arena, playerIndex);
        var isNextChance = nextPlayerIndex == Constants.EnvironmentPlayerIndex;
        var actualNextPlayer = isNextChance ? (byte)Constants.EnvironmentPlayerIndex : (byte)nextPlayerIndex;

        // LOG DEBUG: Round Robin Transition
        Console.WriteLine($"[DEBUG-RR] P{playerIndex} -> Next: {(isNextChance ? "ENV" : "P" + actualNextPlayer)}");

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

            ref var childNode = ref _nodePool[childIndex];
            childNode.PlacementNew(parentIndex, move, hash, actualNextPlayer, isNextChance);

            if (lastChildIndex == -1) parentNode.FirstChildIndex = childIndex;
            else _nodePool[lastChildIndex].NextSiblingIndex = childIndex;

            lastChildIndex = childIndex;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsMoveRisky(in Arena arena, ushort currentHead, byte move)
    {
        var newHead = arena.GetNewHeadPosition(currentHead, move);
        if (!NeighborsGrid.IsValid(newHead)) return true;

        var openExits = 0;
        
        var n = arena.GetNewHeadPosition(newHead, Moves.Up);
        if (NeighborsGrid.IsValid(n) && !arena.Snakes.IsSet(n)) openExits++;
        
        n = arena.GetNewHeadPosition(newHead, Moves.Down);
        if (NeighborsGrid.IsValid(n) && !arena.Snakes.IsSet(n)) openExits++;
        
        n = arena.GetNewHeadPosition(newHead, Moves.Left);
        if (NeighborsGrid.IsValid(n) && !arena.Snakes.IsSet(n)) openExits++;
        
        n = arena.GetNewHeadPosition(newHead, Moves.Right);
        if (NeighborsGrid.IsValid(n) && !arena.Snakes.IsSet(n)) openExits++;

        return openExits < 2;
    }

    private void ExpandChanceNode(int parentIndex, ref Node parentNode, int area)
    {
        Console.WriteLine($"[DEBUG-CHANCE] Expanding Environment Node {parentIndex}");
        CreateEnvironmentChild(parentIndex, area, spawnFood: false);

        if (parentNode.Visits > CHANCE_NODE_VISIT_THRESHOLD)
        {
             CreateEnvironmentChild(parentIndex, area, spawnFood: true);
        }
    }

    private void CreateEnvironmentChild(int parentIndex, int area, bool spawnFood)
    {
        var childIndex = ++_nextId;
        var parentArena = _slotPool.GetArena(parentIndex);
        var childArena = _slotPool.GetArena(childIndex);
        
        childArena.CloneFrom(in parentArena);

        if (spawnFood)
        {
            childArena.SimulateRandomFoodSpawn(_settings.FoodSpawnChance, _settings.MinimumFood, area);
        }

        var hash = ZobristHasher.CalculateHash(in childArena);
        
        ref var childNode = ref _nodePool[childIndex];
        ref var parentNode = ref _nodePool[parentIndex];
        
        // LOG DEBUG: Restart Round Robin
        Console.WriteLine($"[DEBUG-RR] Environment -> P0 (Restarting Round)");

        childNode.PlacementNew(parentIndex, Moves.None, hash, 0, false); // Turno torna a P0

        if (parentNode.FirstChildIndex == -1)
        {
            parentNode.FirstChildIndex = childIndex;
        }
        else
        {
            var sibling = parentNode.FirstChildIndex;
            while (_nodePool[sibling].NextSiblingIndex != -1) sibling = _nodePool[sibling].NextSiblingIndex;
            _nodePool[sibling].NextSiblingIndex = childIndex;
        }
    }

    // --- LOGICA ROUND ROBIN STRUMENTATA ---
    private static int GetNextPlayerIndex(in Arena arena, int currentPlayerIndex)
    {
        // Caso base: siamo all'ultimo serpente della lista -> tocca all'ambiente
        if (currentPlayerIndex >= arena.System.Count - 1)
        {
            Console.WriteLine($"[DEBUG-RR] Player {currentPlayerIndex} is last. Next -> Environment");
            return Constants.EnvironmentPlayerIndex;
        }

        var next = currentPlayerIndex + 1;
        
        // Ciclo per saltare i serpenti morti
        while (next < arena.System.Count && arena.System[next].IsDead)
        {
            Console.WriteLine($"[DEBUG-RR] Skipping Player {next} (DEAD)");
            next++;
        }

        // Se dopo aver saltato i morti siamo usciti dalla lista, tocca all'ambiente
        if (next >= arena.System.Count) 
        {
            Console.WriteLine($"[DEBUG-RR] No more alive players after {currentPlayerIndex}. Next -> Environment");
            return Constants.EnvironmentPlayerIndex;
        }

        Console.WriteLine($"[DEBUG-RR] Next active player after {currentPlayerIndex} is {next}");
        return next;
    }

    // ... [ApplySingleMove, Evaluate, Backpropagate, ecc. rimangono uguali] ...
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ApplySingleMove(in Arena arena, ref WarSnake snake, byte move, int area)
    {
        var newHead = arena.GetNewHeadPosition(snake.Head, move);
        var hasEaten = arena.Food.IsSet(newHead);
        
        var damage = arena.Hazards.IsSet(newHead) ? _settings.HazardDamagePerTurn : 1;

        arena.Snakes.Xor(snake.Body);
        snake.UpdateAfterMove(newHead, hasEaten, damage);
        arena.Snakes.Or(snake.Body);

        if (hasEaten) arena.Food.Unset(newHead);
    }

    private void Evaluate(int nodeIndex, float[] rewardsBuffer)
    {
        var heuristics = _slotPool.GetHeuristics(nodeIndex);
        var arena = _slotPool.GetArena(nodeIndex);

        Array.Clear(rewardsBuffer);

        for(var i=0; i < arena.System.Count; i++)
        {
            var outcome = heuristics.Outcome(i);
            if (outcome != 0.0f)
            {
                rewardsBuffer[i] = outcome; 
            }
        }
        
        Span<float> rawScores = stackalloc float[arena.System.Count];
        heuristics.EvaluateAll(rawScores);

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
            ref var currentNode = ref _nodePool[currentIndex];
            currentNode.UpdateStats(rewards);

            if (currentNode.ParentIndex != -1)
            {
                PropagateSolverFlags(currentIndex, currentNode.ParentIndex);
            }

            currentIndex = currentNode.ParentIndex;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void PropagateSolverFlags(int childIndex, int parentIndex)
    {
        ref var childNode = ref _nodePool[childIndex];
        
        if (childNode.IsSolvedLoss)
        {
            ref var parentNode = ref _nodePool[parentIndex];
            
            if (parentNode.IsSolvedLoss || parentNode.IsSolvedWin) return;
            if (parentNode.IsChanceNode) return;

            var allChildrenLost = true;
            var currentSibling = parentNode.FirstChildIndex;
            while (currentSibling != -1)
            {
                ref var siblingNode = ref _nodePool[currentSibling];
                if (!siblingNode.IsSolvedLoss)
                {
                    allChildrenLost = false;
                    break; 
                }
                currentSibling = siblingNode.NextSiblingIndex;
            }

            if (allChildrenLost)
            {
                parentNode.MarkSolvedLoss();
            }
            return;
        }
    }

    public void Reset(int startId) => _nextId = startId;
    public void Reset(int startId, RulesetSettings settings)
    {
        _nextId = startId;
        _settings = settings;
    }
}