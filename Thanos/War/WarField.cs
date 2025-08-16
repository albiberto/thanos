using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Thanos.MCST; // Assuming these are your project's using statements
using Thanos.SourceGen;

namespace Thanos.War;

[StructLayout(LayoutKind.Sequential)]
public readonly ref struct WarField
{
    public const int TotalBitboards = 3;

    private uint _width { get; }
    private uint _height { get; }
    private uint _area { get; }

    private readonly Bitboard _foodBitboard;
    private readonly Bitboard _hazardsBitboard;
    private readonly Bitboard _snakesBitboard;

    public WarField(in WarContext context, Span<ulong> foodBitboard, Span<ulong> hazardsBitboard, Span<ulong> snakesBitboard, ReadOnlySpan<Coordinate> food, ReadOnlySpan<Coordinate> hazards)
    {
        _width = context.Width;
        _height = context.Height;
        _area = context.Area;
        
        _foodBitboard = new Bitboard(foodBitboard);
        _hazardsBitboard = new Bitboard(hazardsBitboard);
        _snakesBitboard = new Bitboard(snakesBitboard);
        
        // Initialize board state
        foreach (ref readonly var coordinate in food) { _foodBitboard.Set(To1D(in coordinate)); }
        foreach (ref readonly var coordinate in hazards) { _hazardsBitboard.Set(To1D(in coordinate)); }
    }

    // --- WRITE METHODS ---

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetSnakeBit(ushort position1D) => _snakesBitboard.Set(position1D);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UpdateSnakePosition(ushort oldTail, ushort newHead, bool hasEaten)
    {
        var snakes = _snakesBitboard;
        snakes.Set(newHead);
        snakes.Clear(!hasEaten ? oldTail : newHead);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RemoveSnake(ReadOnlySpan<ushort> body)
    {
        foreach (var position in body) _snakesBitboard.Clear(position);
    }

    // --- "HOT PATH" READ METHODS (Safe and highly optimized) ---

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsOccupied(ushort position1D)
    {
        if (position1D == ushort.MaxValue) return true;
        var ulongIndex = position1D >> 6;
        var bitMask = 1UL << (position1D & 63);
        
        // Direct span access via GetRawData() for maximum performance.
        var snakesData = _snakesBitboard.GetRawData();
        var hazardData = _hazardsBitboard.GetRawData();
        
        return ((hazardData[ulongIndex] | snakesData[ulongIndex]) & bitMask) != 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsFood(ushort position1D) => _foodBitboard.IsSet(position1D);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsHazard(ushort position1D) => _hazardsBitboard.IsSet(position1D);

    // --- HELPER METHODS ---

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort To1D(in Coordinate coord) => (ushort)(coord.Y * _width + coord.X);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort GetNeighbor(ushort position1D, MoveDirection direction)
    {
        return direction switch
        {
            MoveDirection.Up => position1D < _width ? ushort.MaxValue : (ushort)(position1D - _width),
            MoveDirection.Down => position1D >= _area - _width ? ushort.MaxValue : (ushort)(position1D + _width),
            MoveDirection.Left => position1D % _width == 0 ? ushort.MaxValue : (ushort)(position1D - 1),
            MoveDirection.Right => (position1D + 1) % _width == 0 ? ushort.MaxValue : (ushort)(position1D + 1),
            _ => ushort.MaxValue
        };
    }
}