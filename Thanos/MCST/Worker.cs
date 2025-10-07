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

    public void RunIteration(int rootIndex)
    {
        var leafIndex = Select(rootIndex);
        ref var leafNode = ref _nodePool[leafIndex];

        if (leafNode is { IsLeafNode: true, IsTerminal: false })
        {
            Expand(leafIndex);
        }

        var outcome = Evaluate(leafIndex);

        Backpropagate(leafIndex, outcome);
    }

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

    private int SelectBestChild(ref Node parentNode)
    {
        var bestScore = double.MinValue;
        var bestChildIndex = -1;
        
        var logParentVisits = Math.Log(parentNode.Visits);

        // LOGGING: Prepara una stringa per il log delle decisioni
        var logBuilder = new StringBuilder();
        logBuilder.Append($"[SelectBestChild] ParentNode (Visits: {parentNode.Visits}):\n");

        var childIndex = parentNode.FirstChildIndex;
        while (childIndex != -1)
        {
            ref var childNode = ref _nodePool[childIndex];

            if (childNode.Visits == 0)
            {
                // Un figlio mai visitato ha priorità assoluta.
                 logBuilder.Append($"  -> Move '{ToApiMove(childNode.Move)}' (Unvisited) - SELECTING\n");
                 Console.WriteLine(logBuilder.ToString()); // Stampa il log prima di uscire
                return childIndex;
            }

            var exploitation = childNode.Wins / childNode.Visits;
            var exploration = EXPLORATION_PARAMETER * Math.Sqrt(logParentVisits / childNode.Visits);
            var uctScore = exploitation + exploration;
            
            logBuilder.Append($"  -> Move '{ToApiMove(childNode.Move)}' | Wins: {childNode.Wins:F2}, Visits: {childNode.Visits}, Exploit: {exploitation:F3}, Explore: {exploration:F3}, UCT: {uctScore:F3}\n");

            if (uctScore > bestScore)
            {
                bestScore = uctScore;
                bestChildIndex = childIndex;
            }

            childIndex = childNode.NextSiblingIndex;
        }
        
        // Stampa il log solo se sono state valutate delle mosse (e non in ogni singola iterazione per non inondare la console)
        // Puoi decommentarlo se vuoi un'analisi estremamente dettagliata
        if(parentNode.Visits % 1000 == 0) Console.WriteLine(logBuilder.ToString());

        return bestChildIndex;
    }

    private void Expand(int parentIndex)
    {
        ref var parentNode = ref _nodePool[parentIndex];
        var parentArena = _slotPool.GetArena(parentIndex);

        var playerIndex = parentArena.PlayerToMoveIndex;
        var playerSnake = parentArena.System[playerIndex];

        if (playerSnake.IsDead)
        {
            parentNode.IsTerminal = true;
            return;
        }
        
        var safeMoves = GetAdvancedLegalMoves(in parentArena, playerIndex);
        
        // LOGGING: Mostra quali mosse sono considerate sicure per l'espansione
        if (safeMoves.Count > 0)
        {
             Console.WriteLine($"[Expand] Node {parentIndex} - Safe moves: {string.Join(", ", safeMoves.Select(ToApiMove))}");
        }


        var lastChildIndex = -1;

        foreach (var move in safeMoves)
        {
            if (_nextId >= Constants.MaxNodes - 1)
            {
                Console.WriteLine("[MCTS] Limite massimo di nodi raggiunto, interrompendo l'espansione.");
                break; 
            }

            var childIndex = ++_nextId;
            var childArena = _slotPool.GetArena(childIndex);
        
            childArena.CloneFrom(in parentArena);

            ApplySingleMove(ref childArena, playerIndex, move);

            childArena.PlayerToMoveIndex = GetNextPlayerIndex(in childArena, playerIndex);

            var hash = ZobristHasher.CalculateHash(in childArena);
            ref var childNode = ref _nodePool[childIndex];
            childNode.PlacementNew(parentIndex, move, hash, parentNode.Generation);

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
    
    private List<byte> GetAdvancedLegalMoves(in Arena arena, int snakeIndex)
    {
        var safeMoves = new List<byte>();
        var mySnake = arena.System[snakeIndex];
        if (mySnake.IsDead) return safeMoves;

        var potentialMoves = arena.GetLegalMoves(mySnake.Head);
        
        foreach (var move in new[] { Moves.Up, Moves.Down, Moves.Left, Moves.Right })
        {
            if ((potentialMoves & move) == 0) continue;

            var myNextHead = arena.GetNewHeadPosition(mySnake.Head, move);
            var isSquareSafe = true;

            for (var i = 0; i < arena.System.Count; i++)
            {
                if (i == snakeIndex) continue;
                var enemySnake = arena.System[i];
                if (enemySnake.IsDead || enemySnake.Length < mySnake.Length) continue;

                var distance = arena.ManhattanDistance(enemySnake.Head, myNextHead);
                if (distance == 1)
                {
                    isSquareSafe = false;
                    break;
                }
            }

            if (isSquareSafe)
            {
                safeMoves.Add(move);
            }
        }

        return safeMoves;
    }

    private static int GetNextPlayerIndex(in Arena arena, int currentPlayerIndex)
    {
        var nextIndex = currentPlayerIndex;
        do
        {
            nextIndex = (nextIndex + 1) % arena.System.Count;
        }
        while (arena.System[nextIndex].IsDead && nextIndex != currentPlayerIndex);

        return nextIndex;
    }

    private void ApplySingleMove(ref Arena arena, int snakeIndex, byte move)
    {
        var snake = arena.System[snakeIndex];
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
        
        // LOGGING: Stampa il punteggio dell'euristica per il nodo foglia
        // Puoi decommentarlo per vedere il valore di ogni simulazione
        Console.WriteLine($"[Evaluate] Node {leafIndex} - Heuristic score: {score:F2}");

        return score;
    }
    
    private void Backpropagate(int startNodeIndex, float rawScore)
    {
        const float scalingFactor = 100.0f;
        var normalizedResult = MathF.Tanh(rawScore / scalingFactor);
        
        var scoreToPropagate = normalizedResult;
        var currentIndex = startNodeIndex;

        while (currentIndex != -1)
        {
            ref var currentNode = ref _nodePool[currentIndex];
            currentNode.UpdateStats(scoreToPropagate);
            
            scoreToPropagate *= -1;
            
            currentIndex = currentNode.ParentIndex;
        }
    }
    
    // Metodo helper per convertire la mossa in stringa per i log
    private static string ToApiMove(byte move) =>
        move switch
        {
            Moves.Up => "up",
            Moves.Down => "down",
            Moves.Left => "left",
            Moves.Right => "right",
            _ => "none"
        };

    public void Reset(int startId) => _nextId = startId;
    public void Reset(int startId, RulesetSettings settings)
    {
        _nextId = startId;
        _settings = settings;
    }
}