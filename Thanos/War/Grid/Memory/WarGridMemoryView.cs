using System.Runtime.InteropServices;

namespace Thanos.War.Grid.Memory;

public readonly ref struct WarGridMemoryView(Span<byte> memory, in WarGridMemoryLayout layout)
{
    private readonly Span<byte> _memory = memory;
    private readonly WarGridMemoryLayout _layout = layout;
    
    public ref Geography Geography => 
        ref MemoryMarshal.GetReference(MemoryMarshal.Cast<byte, Geography>(_memory[.._layout.GeographySize]));

    public Bitboard Food => 
        new(MemoryMarshal.Cast<byte, ulong>(_memory.Slice(_layout.GeographySize, _layout.BitboardStrideInBytes)));
            
    public Bitboard Hazards => 
        new(MemoryMarshal.Cast<byte, ulong>(_memory.Slice(_layout.GeographySize + _layout.BitboardStrideInBytes, _layout.BitboardStrideInBytes)));

    public Bitboard Snakes => 
        new(MemoryMarshal.Cast<byte, ulong>(_memory.Slice(_layout.GeographySize + _layout.BitboardStrideInBytes * 2, _layout.BitboardStrideInBytes)));
}