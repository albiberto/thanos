using System.Runtime.InteropServices;
using Thanos.War.Grid;

namespace Thanos.War.Memory.Views;

public readonly ref struct WarGridMemoryView(Span<byte> memory, in WarGridMemoryLayout layout)
{
    private readonly Span<byte> _memory = memory;
    private readonly WarGridMemoryLayout _layout = layout; // <- Nota il tipo corretto

    public Bitboard Food => new(MemoryMarshal.Cast<byte, ulong>(_memory.Slice(_layout.FoodOffset, _layout.BitboardSize)));

    public Bitboard Hazards => new(MemoryMarshal.Cast<byte, ulong>(_memory.Slice(_layout.HazardsOffset, _layout.BitboardSize)));

    public Bitboard Snakes => new(MemoryMarshal.Cast<byte, ulong>(_memory.Slice(_layout.SnakesOffset, _layout.BitboardSize)));
}