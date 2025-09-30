using System.Collections.Generic;
using System.Linq;
using Thanos.Common;
using Thanos.Memory;
using Thanos.SourceGen;

namespace Thanos.MCST;

public sealed class Worker(SlotMemoryPool slotPool, NodeMemoryPool nodePool)
{
    private const double EXPLORATION_PARAMETER = 1.41;
    private const double HEURISTIC_WEIGHT = 0.5;
    private static readonly byte[] AllMovesArray = [Moves.Up, Moves.Down, Moves.Left, Moves.Right];

    private int _nextId = 1;
    private readonly NodeMemoryPool _nodePool = nodePool;
    private RulesetSettings _settings;

    public void RunIteration(int rootIndex)
    {
        var leafIndex = Select(rootIndex);
        ref var leafNode = ref _nodePool[leafIndex];

        if (leafNode is { IsLeafNode: true, IsTerminal: false }) Expand(leafIndex);

        var outcome = Evaluate(leafIndex);

        Backpropagate(leafIndex, outcome);
    }

    private int Select(int rootIndex)
    {
        var currentIndex = rootIndex;
        while (true)
        {
            ref var currentNode = ref _nodePool[currentIndex];
            if (currentNode.IsLeafNode || currentNode.IsTerminal) return currentIndex;

            var candidateIndex = SelectBestChild(ref currentNode);
            if (candidateIndex == -1) throw new InvalidOperationException("SelectBestChild returned -1 in a non-leaf node.");

            currentIndex = candidateIndex;
        }
    }

    private int SelectBestChild(ref Node parentNode)
    {
        var bestScore = double.MinValue;
        var bestChildIndex = -1;

        if (parentNode.Visits == 0) return parentNode.FirstChildIndex; // Visit the first child if the parent is new.
        
        var logParentVisits = Math.Log(parentNode.Visits);

        var childIndex = parentNode.FirstChildIndex;
        while (childIndex != -1)
        {
            ref var childNode = ref _nodePool[childIndex];

            // A never-visited child always has absolute priority.
            if (childNode.Visits == 0) return childIndex;

            var exploitation = childNode.Wins / childNode.Visits;
            var exploration = EXPLORATION_PARAMETER * Math.Sqrt(logParentVisits / childNode.Visits);
            var uctScore = exploitation + exploration;
            
            // Note: Heuristic component has been removed from here to follow your request.
            // The final score is just the UCT score.

            if (uctScore > bestScore)
            {
                bestScore = uctScore;
                bestChildIndex = childIndex;
            }

            childIndex = childNode.NextSiblingIndex;
        }

        return bestChildIndex;
    }

    private void Expand(int parentIndex)
{
    ref var parentNode = ref _nodePool[parentIndex];
    var parentArena = slotPool.GetArena(parentIndex);
    var parentGeneration = parentNode.Generation;

    if (parentArena.System.Me.IsDead)
    {
        parentNode.IsTerminal = true;
        return;
    }

    // Calcolo delle mosse legali per tutti i serpenti
    var allLegalMoves = new Dictionary<int, List<byte>>();
    for (var i = 0; i < parentArena.System.Count; i++)
    {
        var snake = parentArena.System[i];
        if (!snake.IsDead)
        {
            allLegalMoves[i] = new List<byte>();
            var moves = parentArena.GetLegalMoves(snake.Head);
            if ((moves & Moves.Up) != 0) allLegalMoves[i].Add(Moves.Up);
            if ((moves & Moves.Down) != 0) allLegalMoves[i].Add(Moves.Down);
            if ((moves & Moves.Left) != 0) allLegalMoves[i].Add(Moves.Left);
            if ((moves & Moves.Right) != 0) allLegalMoves[i].Add(Moves.Right);
        }
    }

    var combinations = GenerateCombinations(allLegalMoves);
    var lastChildIndex = -1;

    foreach (var combination in combinations)
    {
        if (!combination.ContainsKey(0)) continue;

        var childIndex = ++_nextId;
        var childArena = slotPool.GetArena(childIndex);
        childArena.CloneFrom(in parentArena);

        var newHeads = new Dictionary<int, ushort>();
        var headsInSameSquare = new Dictionary<ushort, List<int>>();
        var snakesToKill = new HashSet<int>();

        // FASE 1: RILEVAMENTO DELLE COLLISIONI (le mosse non vengono applicate)
        foreach (var snakeMove in combination)
        {
            var snakeIndex = snakeMove.Key;
            var move = snakeMove.Value;
            var snake = childArena.System[snakeIndex];

            if (snake.IsDead) continue;

            var newHead = childArena.GetNewHeadPosition(snake.Head, move);
            newHeads[snakeIndex] = newHead;

            // Collisione con i muri
            if (!childArena.IsValidPosition(newHead))
            {
                snakesToKill.Add(snakeIndex);
                continue;
            }

            // Rilevamento collisione testa-corpo (inclusa la coda non ancora liberata)
            if (childArena.Snakes.IsSet(newHead))
            {
                var isMovingToOwnVacatingTail = newHead == snake.Tail && !childArena.Food.IsSet(newHead);
                if (!isMovingToOwnVacatingTail)
                {
                    snakesToKill.Add(snakeIndex);
                }
            }

            // Rilevamento collisione testa-testa
            if (headsInSameSquare.ContainsKey(newHead))
            {
                headsInSameSquare[newHead].Add(snakeIndex);
            }
            else
            {
                headsInSameSquare[newHead] = new List<int> { snakeIndex };
            }
        }

        // Risoluzione delle collisioni testa-testa
        foreach (var collision in headsInSameSquare.Values.Where(c => c.Count > 1))
        {
            var maxLength = -1;
            foreach (var snakeIndex in collision)
            {
                var snake = childArena.System[snakeIndex];
                if (!snakesToKill.Contains(snakeIndex) && snake.Length > maxLength)
                {
                    maxLength = snake.Length;
                }
            }

            foreach (var snakeIndex in collision)
            {
                var snake = childArena.System[snakeIndex];
                if (!snakesToKill.Contains(snakeIndex) && snake.Length < maxLength)
                {
                    snakesToKill.Add(snakeIndex);
                }
            }
        }

        // FASE 2: APPLICAZIONE DELLE MOSSE E AGGIORNAMENTO DELLO STATO
        foreach (var snakeIndex in combination.Keys)
        {
            var snake = childArena.System[snakeIndex];
            
            // Se il serpente è già morto o verrà ucciso in questo turno, applica la morte
            if (snakesToKill.Contains(snakeIndex) || snake.IsDead)
            {
                snake.Kill();
                childArena.Snakes.Xor(snake.Body);
            }
            else
            {
                // Solo i sopravvissuti applicano la mossa
                var newHead = newHeads[snakeIndex];
                var hasEaten = childArena.Food.IsSet(newHead);
                var damage = childArena.Hazards.IsSet(newHead) ? 10 : 1;
                var newTail = childArena.CalculateNewTailPosition(snake, hasEaten);
                
                // Aggiorna lo stato del serpente
                childArena.Snakes.Xor(snake.Body);
                snake.UpdateAfterMove(newHead, newTail, hasEaten, damage);
                childArena.Snakes.Or(snake.Body);
                
                // Rimuovi il cibo mangiato
                if (hasEaten) childArena.Food.Unset(newHead);
            }
        }
        
        // Simula lo spawn di cibo dopo che tutte le mosse sono state elaborate
        childArena.SimulateRandomFoodSpawn(_settings.FoodSpawnChance, _settings.MinimumFood);
        var hash = ZobristHasher.CalculateHash(in childArena);

        // Creazione del nodo e collegamento all'albero
        ref var childNode = ref _nodePool[childIndex];
        childNode.PlacementNew(parentIndex, combination[0], hash, parentGeneration);

        if (lastChildIndex == -1)
        {
            parentNode.FirstChildIndex = childIndex;
        }
        else
        {
            ref var lastChildNode = ref _nodePool[lastChildIndex];
            lastChildNode.NextSiblingIndex = childIndex;
        }

        lastChildIndex = childIndex;
    }

    if (lastChildIndex == -1)
    {
        parentNode.IsTerminal = true;
    }
}

    private List<Dictionary<int, byte>> GenerateCombinations(Dictionary<int, List<byte>> legalMoves)
    {
        var allSnakesIndices = legalMoves.Keys.ToList();
        var combinations = new List<Dictionary<int, byte>>();
        GenerateCombinationsRecursive(allSnakesIndices, legalMoves, new Dictionary<int, byte>(), 0, combinations);
        return combinations;
    }

    private void GenerateCombinationsRecursive(
        List<int> snakeIndices,
        Dictionary<int, List<byte>> legalMoves,
        Dictionary<int, byte> currentCombination,
        int snakeIndex,
        List<Dictionary<int, byte>> results)
    {
        if (snakeIndex == snakeIndices.Count)
        {
            results.Add(new Dictionary<int, byte>(currentCombination));
            return;
        }

        var currentSnakeIndex = snakeIndices[snakeIndex];
        if (legalMoves.TryGetValue(currentSnakeIndex, out var moves))
        {
            foreach (var move in moves)
            {
                currentCombination[currentSnakeIndex] = move;
                GenerateCombinationsRecursive(snakeIndices, legalMoves, currentCombination, snakeIndex + 1, results);
            }
        }
        else
        {
            // If snake is dead or has no legal moves, skip them.
            GenerateCombinationsRecursive(snakeIndices, legalMoves, currentCombination, snakeIndex + 1, results);
        }
    }

    private float Evaluate(int leafIndex)
    {
        var heuristics = slotPool.GetHeuristics(leafIndex);
        var outcome = heuristics.Outcome();
        return outcome != 0.0f
            ? outcome
            : heuristics.Evaluate();
    }

    private void Backpropagate(int startNodeIndex, float rawScore)
    {
        const float scalingFactor = 100.0f;
        var normalizedResult = MathF.Tanh(rawScore / scalingFactor);

        var currentIndex = startNodeIndex;
        while (currentIndex != -1)
        {
            ref var currentNode = ref _nodePool[currentIndex];
            currentNode.UpdateStats(normalizedResult);
            currentIndex = currentNode.ParentIndex;
        }
    }

    // Existing helper methods...
    public int GetMaxId(int rootIndex)
    {
        if (rootIndex == 0) return 0;
        var maxId = rootIndex;
        var queue = new Queue<int>();
        queue.Enqueue(rootIndex);
        while (queue.Count > 0)
        {
            var currentIndex = queue.Dequeue();
            maxId = Math.Max(maxId, currentIndex);
            ref var currentNode = ref _nodePool[currentIndex];
            var childIndex = currentNode.FirstChildIndex;
            while (childIndex != -1)
            {
                queue.Enqueue(childIndex);
                childIndex = _nodePool[childIndex].NextSiblingIndex;
            }
        }
        return maxId;
    }

    public void Reset(int startId) => _nextId = startId;
    public void Reset(int startId, RulesetSettings settings)
    {
        _nextId = startId;
        _settings = settings;
    }
}