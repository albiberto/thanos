using System.Runtime.InteropServices;
using Thanos.SourceGen;

namespace Thanos.War.Grid;


[StructLayout(LayoutKind.Sequential)]
public readonly ref struct WarGrid
{
    public readonly ref Geography Geography;

    public readonly Bitboard Food;
    public readonly Bitboard Hazards;
    public readonly Bitboard Snakes;

    public WarGrid(ref Geography geography, Span<ulong> foodBitboard, Span<ulong> hazardsBitboard, Span<ulong> snakesBitboard)
    {
        Geography = ref geography;
        
        Food = new Bitboard(foodBitboard);
        Hazards = new Bitboard(hazardsBitboard);
        Snakes = new Bitboard(snakesBitboard);
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
}