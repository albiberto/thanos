using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Thanos.MCST;
using Thanos.War.Grid.Memory;

namespace Thanos.War.Grid;

[StructLayout(LayoutKind.Sequential)]
public readonly ref struct WarGrid
{
    public readonly ref Geography Geography;

    private readonly Bitboard _food;
    private readonly Bitboard _hazards;
    private readonly Bitboard _snakes;

    private readonly ReadOnlySpan<ushort> _neighborsBoard;

    public WarGrid(WarGridMemoryView view)
    {
        Geography = ref view.Geography;

        _food = view.Food;
        _hazards = view.Hazards;
        _snakes = view.Snakes;

        _neighborsBoard = view.NeighborsBoard;
    }

    public bool IsOccupied(ushort position) => position == ushort.MaxValue || _snakes.IsSet(position);

    public bool IsFood(ushort position) => _food.IsSet(position);

    public bool IsHazard(ushort position) => _hazards.IsSet(position);

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
}