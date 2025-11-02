using System.Runtime.CompilerServices;
using Thanos.Common;
using Thanos.Memory;
using Thanos.SourceGen;
using Thanos.War;

// Aggiunto per StringBuilder

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
    public void RunIteration(int area, int rootIndex)
    {
        var leafIndex = Select(rootIndex);
        ref var leafNode = ref _nodePool[leafIndex];

        if (leafNode is { IsLeafNode: true, IsTerminal: false }) Expand(leafIndex, ref leafNode, area);

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

            if (currentNode.IsLeafNode || currentNode.IsTerminal) return currentIndex;

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
        
        // Determina se stiamo scegliendo per noi (0) o per un nemico (!= 0)
        var isMyTurn = parentNode.PlayerIndex == 0;

        var childIndex = parentNode.FirstChildIndex;
        while (childIndex != -1)
        {
            ref var childNode = ref _nodePool[childIndex];

            if (childNode.Visits == 0) return childIndex; // Nodo non esplorato, priorità (UCB)

            // 1. Calcola l'exploitation score dal *nostro* punto di vista (Player 0)
            var rawExploitation = childNode.Wins / childNode.Visits;

            // 2. CORREZIONE NEGAMAX: Inverti la prospettiva se è il turno del nemico
            // Se è il nostro turno, massimizziamo.
            // Se è il turno del nemico, lui massimizzerà il *suo* punteggio,
            // che equivale a *minimizzare* il nostro.
            var exploitation = isMyTurn ? rawExploitation : -rawExploitation;

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
    private void Expand(int parentIndex, ref Node parentNode, int area)
    {
        var playerIndex = parentNode.PlayerIndex;

        var playerArena = _slotPool.GetArena(parentIndex);
        var playerSnake = playerArena.System[playerIndex];

        if (playerSnake.IsDead)
        {
            parentNode.IsTerminal = true;
            return;
        }

        var safeMoves = playerArena.GetLegalMoves(playerSnake.Head, playerSnake.Tail, playerSnake.ElementBeforeTail);

        ExpandNode(parentIndex, safeMoves, ref parentNode, in playerArena, area);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ExpandNode(int parentIndex, byte safeMoves, ref Node parentNode, in Arena parentArena, int area)
    {
        var playerIndex = parentNode.PlayerIndex;
        var lastChildIndex = -1;

        foreach (var move in AllMoves)
        {
            if ((safeMoves & move) == 0) continue;

            var childIndex = ++_nextId;
            var childArena = _slotPool.GetArena(childIndex);

            childArena.CloneFrom(in parentArena);

            var snakeToMove = childArena.System[playerIndex];
            ApplySingleMove(in childArena, ref snakeToMove, move, area);

            var nextPlayerIndex = GetNextPlayerIndex(in childArena, playerIndex);

            var hash = ZobristHasher.CalculateHash(in childArena);
            ref var childNode = ref _nodePool[childIndex];
            childNode.PlacementNew(parentIndex, move, hash, nextPlayerIndex);

            if (lastChildIndex == -1)
                parentNode.FirstChildIndex = childIndex;
            else
                _nodePool[lastChildIndex].NextSiblingIndex = childIndex;

            lastChildIndex = childIndex;
        }

        if (lastChildIndex == -1) parentNode.IsTerminal = true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte GetNextPlayerIndex(in Arena arena, int currentPlayerIndex)
    {
        var nextPlayerIndex = currentPlayerIndex;

        do
        {
            nextPlayerIndex = (nextPlayerIndex + 1) % arena.System.Count;
        } while (arena.System[nextPlayerIndex].IsDead && nextPlayerIndex != currentPlayerIndex);

        return (byte)nextPlayerIndex;
    }

// *** METODO CHIAVE AGGIORNATO ***
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ApplySingleMove(in Arena arena, ref WarSnake snake, byte move, int area)
    {
        var newHead = arena.GetNewHeadPosition(snake.Head, move);

        var hasEaten = arena.Food.IsSet(newHead);
        var damage = arena.Hazards.IsSet(newHead) ? _settings.HazardDamagePerTurn : 1;

        // 1. Rimuovi il corpo del serpente corrente dalla bitboard globale
        arena.Snakes.Xor(snake.Body);

        // 2. Aggiorna lo stato interno del serpente (posizione e bitboard)
        //    Questa chiamata ora gestisce la coda e la crescita internamente.
        snake.UpdateAfterMove(newHead, hasEaten, damage);

        // 3. Aggiungi il nuovo corpo del serpente alla bitboard globale
        arena.Snakes.Or(snake.Body);

        if (hasEaten) arena.Food.Unset(newHead);

        // La logica per lo spawn del cibo rimane, ma potrebbe essere semplificata in futuro
        // arena.SimulateRandomFoodSpawn(_settings.FoodSpawnChance, _settings.MinimumFood, area);
    }

    private float Evaluate(int leafIndex)
    {
        var heuristics = _slotPool.GetHeuristics(leafIndex);
        var outcome = heuristics.Outcome();
        var score = outcome != 0.0f ? outcome : heuristics.Evaluate();

        return score;
    }

    private void Backpropagate(int startNodeIndex, float outcome)
    {
        const float scalingFactor = 100.0f;
        var scoreToPropagate = MathF.Tanh(outcome / scalingFactor);

        var currentIndex = startNodeIndex;
        
        ref var startNode = ref _nodePool[currentIndex];
        
        if (startNode.PlayerIndex != 0) scoreToPropagate *= -1;

        while (currentIndex != -1)
        {
            ref var currentNode = ref _nodePool[currentIndex];
            currentNode.UpdateStats(scoreToPropagate);

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