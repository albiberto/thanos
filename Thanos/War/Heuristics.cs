// Thanos/War/Heuristics.cs

using System.Numerics;
using System.Runtime.CompilerServices;
using Thanos.Common;
using Thanos.SourceGen;

namespace Thanos.War;

public static class HeuristicsConstants
{
    public const float SpaceWeight = 10.5f;
    public const float HealthWeight = 0.5f;
    public const float FoodWeight = 0.6f;
    public const float CenterBonusValue = 15.0f;
    public const float BorderPenaltyValue = -100.0f;
}

public readonly ref struct Heuristics
{
    private readonly SnakesSystem _system;
    private readonly Bitboard _food;
    private readonly Bitboard _hazards;
    private readonly Bitboard _snakes;
    private readonly NeighborsGrid _neighborsGrid;
    private readonly ReadOnlySpan<Coordinate> _conversionsMap;
    private readonly ReadOnlySpan<float> _positionalScores;

    private static readonly byte[] AllMovesArray = [Moves.Up, Moves.Down, Moves.Left, Moves.Right];

    public Heuristics(SnakesSystem system, Bitboard food, Bitboard hazards, Bitboard snakes, NeighborsGrid neighborsGrid, ReadOnlySpan<Coordinate> conversionsMap, ReadOnlySpan<float> positionalScores)
    {
        _system = system;
        _food = food;
        _hazards = hazards;
        _snakes = snakes;
        _neighborsGrid = neighborsGrid;
        _conversionsMap = conversionsMap;
        _positionalScores = positionalScores;
    }

    public float Outcome()
    {
        var me = _system.Me;
        if (me.IsDead) return -1.0f;

        for (var i = 1; i < _system.Count; i++)
        {
            if (!_system[i].IsDead)
                return 0.0f;
        }

        return 1.0f;
    }

    public float Evaluate()
    {
        var me = _system.Me;
        if (me.IsDead) return float.NegativeInfinity;

        var head = me.Head;
        if (head >= _positionalScores.Length) return float.NegativeInfinity;

        var myLength = me.Length;
        // CORREZIONE: La proprietà era HP, ora è Health
        var health = me.HP;
        var score = 0.0f;
        
        score += _positionalScores[head];
        score -= Head2HeadCollision(myLength, head);
        score -= PenalityTrap(head);
        score += health * HeuristicsConstants.HealthWeight;

        // --- EURISTICA DELLO SPAZIO (Flood Fill) ---
        var walls = _snakes;
        Span<byte> wallsMemoryCopy = stackalloc byte[walls.Raw.Length];
        walls.Raw.CopyTo(wallsMemoryCopy);
        var simulatedWalls = new Bitboard(wallsMemoryCopy);
        
        // CORREZIONE: La proprietà 'WillGrow' non esiste più.
        // Assumiamo lo scenario più comune in cui il serpente non mangia e quindi la coda si sposta,
        // liberando una casella. Questo è fondamentale per non sottostimare lo spazio disponibile.
        simulatedWalls.Unset(me.Tail);
        
        var mySpace = FloodFill(head, simulatedWalls);
        score += mySpace * HeuristicsConstants.SpaceWeight;

        // --- EURISTICA DEL CIBO ---
        // CORREZIONE: La Bitboard non espone più 'Memory', usiamo la proprietà 'Raw'
        var foodBitboard = _food.Memory;
        var headCoord = _conversionsMap[head];
        score += HeuristicsConstants.FoodWeight * CalculateFoodIncentive(headCoord, health, foodBitboard, _conversionsMap);

        return score;
    }

    private float PenalityTrap(ushort head)
    {
        var openExits = 0;
        var currentWalls = _snakes;
        foreach (var move in AllMovesArray)
        {
            var neighbor = _neighborsGrid.Get(head, move);
            if (NeighborsGrid.IsValid(neighbor) && !currentWalls.IsSet(neighbor)) openExits++;
        }

        return openExits switch
        {
            <= 1 => 750.0f,
            2 => 200.0f,
            _ => 0
        };
    }

    private float Head2HeadCollision(int myLength, ushort head)
    {
        for (var i = 1; i < _system.Count; i++)
        {
            var enemy = _system[i];
            if (enemy.IsDead || enemy.Length < myLength) continue;

            var enemyHead = enemy.Head;
            if (_neighborsGrid.Get(head, Moves.Up) == enemyHead ||
                _neighborsGrid.Get(head, Moves.Down) == enemyHead ||
                _neighborsGrid.Get(head, Moves.Left) == enemyHead ||
                _neighborsGrid.Get(head, Moves.Right) == enemyHead)
            {
                return 25000.0f;
            }
        }
        return 0.0f;
    }

    private static float CalculateFoodIncentive(Coordinate head, int health, ReadOnlySpan<ulong> food, ReadOnlySpan<Coordinate> map)
    {
        var distance = int.MaxValue;

        for (var i = 0; i < food.Length; i++)
        {
            var chunk = food[i];
            if (chunk == 0) continue;

            while (chunk != 0)
            {
                var bitIndex = BitOperations.TrailingZeroCount(chunk);
                var pos1D = (ushort)((i << 6) + bitIndex);

                var foodCoords = map[pos1D];
                var d = Abs(head.X - foodCoords.X) + Abs(head.Y - foodCoords.Y);
                if (d < distance)
                {
                    distance = d;
                    if (distance == 1) goto EndLoop;
                }

                chunk &= ~(1UL << bitIndex);
            }
        }

    EndLoop:
        if (distance is >= int.MaxValue or 0) return 0.0f;
        
        var urgency = 101.0f - health;
        return urgency / distance;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int Abs(int n)
    {
        var mask = n >> 31;
        return (n + mask) ^ mask;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [SkipLocalsInit]
    private int FloodFill(ushort startNode, in Bitboard walls)
    {
        if (walls.IsSet(startNode)) return 0;
        
        const int MaxStackSize = 256;
        Span<ushort> stack = stackalloc ushort[MaxStackSize];
        
        Span<byte> visitedMemory = stackalloc byte[MaxStackSize / 8];
        visitedMemory.Clear();
        var visited = new Bitboard(visitedMemory);

        stack[0] = startNode;
        var stackPointer = 1;
        visited.Set(startNode);
        var count = 1;

        while (stackPointer > 0)
        {
            var current = stack[--stackPointer];
            foreach (var move in AllMovesArray)
            {
                var neighbor = _neighborsGrid.Get(current, move);
                if (!NeighborsGrid.IsValid(neighbor) || walls.IsSet(neighbor) || visited.IsSet(neighbor)) continue;
                visited.Set(neighbor);
                stack[stackPointer++] = neighbor;
                count++;
            }
        }

        return count;
    }
}