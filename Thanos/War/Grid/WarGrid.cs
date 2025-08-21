using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Thanos.SourceGen;

namespace Thanos.War.Grid;


[StructLayout(LayoutKind.Sequential)]
public readonly ref struct WarGrid
{
    public const int TotalBitboards = 3;

    public int Width { get; }
    public int Height { get; }
    public int Area { get; }

    public readonly Bitboard Food;
    public readonly Bitboard Hazards;
    public readonly Bitboard Snakes;

    public WarGrid(int width, int height, int area, Span<ulong> foodBitboard, Span<ulong> hazardsBitboard, Span<ulong> snakesBitboard)
    {
        Width = width;
        Height = height;
        Area = area;
        
        Food = new Bitboard(foodBitboard);
        Hazards = new Bitboard(hazardsBitboard);
        Snakes = new Bitboard(snakesBitboard);
    }
    
    public WarGrid(int width, int height, int area, Span<ulong> foodBitboard, Span<ulong> hazardsBitboard, Span<ulong> snakesBitboard, ReadOnlySpan<Coordinate> food, ReadOnlySpan<Coordinate> hazards)
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

    public bool IsOccupied(ushort position1D)
    {
        if (position1D == ushort.MaxValue) return true;
        var ulongIndex = position1D >> 6;
        var bitMask = 1UL << (position1D & 63);
        
        var snakesData = Snakes.GetRawData();
        return (snakesData[ulongIndex] & bitMask) != 0;
    }

    public bool IsFood(ushort position1D) => Food.IsSet(position1D);

    public bool IsHazard(ushort position1D) => Hazards.IsSet(position1D);

    // --- HELPER METHODS ---

    public ushort To1D(in Coordinate coord) => To1D(coord, Width);
    
    public static ushort To1D(in Coordinate coord, int width) => (ushort)(coord.Y * width + coord.X);
}