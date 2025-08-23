using System.Runtime.InteropServices;
using Thanos.SourceGen;
using Thanos.War.Grid.Memory;

namespace Thanos.War.Grid;


[StructLayout(LayoutKind.Sequential)]
public readonly ref struct WarGrid
{
    public readonly ref Geography Geography;

    public readonly Bitboard Food;
    public readonly Bitboard Hazards;
    public readonly Bitboard Snakes;

    public WarGrid(WarGridMemoryView view)
    {
        Geography = ref view.Geography;
        
        Food = view.Food;
        Hazards = view.Hazards;
        Snakes = view.Snakes;
    }
    
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