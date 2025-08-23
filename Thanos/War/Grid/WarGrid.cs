using System.Numerics;
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

    public readonly ReadOnlySpan<ushort> _neighborsBoard;

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
}