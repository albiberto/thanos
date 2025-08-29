using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Thanos.Common;
using Thanos.MCST;
using Thanos.War.Grid.Memory;
using Thanos.War.Snake;

namespace Thanos.War.Grid;

[StructLayout(LayoutKind.Sequential)]
public readonly ref struct WarGrid
{
    public readonly ref Geography Geography;

    public readonly Bitboard Food;
    public readonly Bitboard Hazards;
    public readonly Bitboard Snakes;

    private readonly ReadOnlySpan<ushort> _neighborsBoard;

    public WarGrid(WarGridMemoryView view)
    {
        Geography = ref view.Geography;

        Food = view.Food;
        Hazards = view.Hazards;
        Snakes = view.Snakes;

        _neighborsBoard = view.NeighborsBoard;
    }

    public bool IsOccupied(ushort position) => position == ushort.MaxValue || Snakes.IsSet(position);

    public bool IsFood(ushort position) => Food.IsSet(position);

    public bool IsHazard(ushort position) => Hazards.IsSet(position);

    public ushort GetNeighbor(ushort position, byte move) => _neighborsBoard[position * 4 + BitOperations.TrailingZeroCount(move)];

    public byte GetLegalMoves(ushort headPosition)
    {
        // Step 1: Get all potential neighbor positions from the LUT.
        var upPos = GetNeighbor(headPosition, Moves.Up);
        var downPos = GetNeighbor(headPosition, Moves.Down);
        var leftPos = GetNeighbor(headPosition, Moves.Left);
        var rightPos = GetNeighbor(headPosition, Moves.Right);

        // Step 2: Check each position and convert the boolean result to a byte (0 or 1).
        var isUpValid = !IsOccupied(upPos);
        var upValid = Unsafe.As<bool, byte>(ref isUpValid);

        var isDownValid = !IsOccupied(downPos);
        var downValid = Unsafe.As<bool, byte>(ref isDownValid);

        var isLeftValid = !IsOccupied(leftPos);
        var leftValid = Unsafe.As<bool, byte>(ref isLeftValid);

        var isRightValid = !IsOccupied(rightPos);
        var rightValid = Unsafe.As<bool, byte>(ref isRightValid);

        // Step 3: Combine the results into a final bitmask.
        return (byte)((upValid * Moves.Up) | (downValid * Moves.Down) | (leftValid * Moves.Left) | (rightValid * Moves.Right));
    }
    
    public void UpdateSnakePosition(ushort oldTail, ushort newHead, bool hasEaten)
    {
        if (!hasEaten) Snakes.Unset(oldTail);
        Snakes.Set(newHead);
    }
    
    public void RemoveFood(ushort position) => Food.Unset(position);

    public void RemoveSnake(WarSnake snake)
    {
        snake.GetSpans(out var bodyFirst, out var bodySecond);
        foreach (var pos in bodyFirst) Snakes.Unset(pos);
        foreach (var pos in bodySecond) Snakes.Unset(pos);
    }
}