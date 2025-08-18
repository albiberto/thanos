using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Thanos.MCST; // Assuming these are your project's using statements
using Thanos.SourceGen;

namespace Thanos.War;

public readonly ref struct Bitboard(Span<ulong> bitboard)
{
    private readonly Span<ulong> _bitboard = bitboard;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<ulong> GetRawData() => _bitboard;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Set(ushort position1D) => _bitboard[position1D >> 6] |= 1UL << (position1D & 63);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear(ushort position1D) => _bitboard[position1D >> 6] &= ~(1UL << (position1D & 63));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsSet(ushort position1D)
    {
        var index = position1D >> 6;
        var mask = 1UL << (position1D & 63);
        return (_bitboard[index] & mask) != 0;
    }
    
    public void ClearAll() => _bitboard.Clear();
}

[StructLayout(LayoutKind.Sequential)]
public readonly ref struct WarField
{
    public const int TotalBitboards = 3;

    public int Width { get; }
    public int Height { get; }
    public int Area { get; }

    public readonly Bitboard Food;
    public readonly Bitboard Hazards;
    public readonly Bitboard Snakes;

    public WarField(int width, int height, int area, Span<ulong> foodBitboard, Span<ulong> hazardsBitboard, Span<ulong> snakesBitboard)
    {
        Width = width;
        Height = height;
        Area = area;
        
        Food = new Bitboard(foodBitboard);
        Hazards = new Bitboard(hazardsBitboard);
        Snakes = new Bitboard(snakesBitboard);
    }
    
    public WarField(int width, int height, int area, Span<ulong> foodBitboard, Span<ulong> hazardsBitboard, Span<ulong> snakesBitboard, ReadOnlySpan<Coordinate> food, ReadOnlySpan<Coordinate> hazards)
    {
        Width = width;
        Height = height;
        Area = area;
        
        Food = new Bitboard(foodBitboard);
        Hazards = new Bitboard(hazardsBitboard);
        Snakes = new Bitboard(snakesBitboard);
        
        // Initialize board state
        foreach (ref readonly var coordinate in food) { Food.Set(To1D(in coordinate)); }
        foreach (ref readonly var coordinate in hazards) { Hazards.Set(To1D(in coordinate)); }
    }

    // --- "HOT PATH" READ METHODS (Safe and highly optimized) ---

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsOccupied(ushort position1D)
    {
        if (position1D == ushort.MaxValue) return true;
        var ulongIndex = position1D >> 6;
        var bitMask = 1UL << (position1D & 63);
        
        // Direct span access via GetRawData() for maximum performance.
        var snakesData = Snakes.GetRawData();
        var hazardData = Hazards.GetRawData();
        
        return ((hazardData[ulongIndex] | snakesData[ulongIndex]) & bitMask) != 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsFood(ushort position1D) => Food.IsSet(position1D);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsHazard(ushort position1D) => Hazards.IsSet(position1D);

    // --- HELPER METHODS ---

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort To1D(in Coordinate coord) => (ushort)(coord.Y * Width + coord.X);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort GetNeighbor(ushort position1D, byte move) =>
        move switch
        {
            Moves.Up => position1D < Width ? ushort.MaxValue : (ushort)(position1D - Width),
            Moves.Down => position1D >= Area - Width ? ushort.MaxValue : (ushort)(position1D + Width),
            Moves.Left => position1D % Width == 0 ? ushort.MaxValue : (ushort)(position1D - 1),
            Moves.Right => (position1D + 1) % Width == 0 ? ushort.MaxValue : (ushort)(position1D + 1),
            _ => ushort.MaxValue
        };
}