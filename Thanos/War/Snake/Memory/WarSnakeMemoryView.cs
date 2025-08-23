using System.Runtime.InteropServices;

namespace Thanos.War.Snake.Memory;

public readonly ref struct WarSnakeMemoryView(Span<byte> memory, in WarSnakeMemoryLayout layout, int id)
{
    private readonly Span<byte> _memory = memory.Slice(id * layout.Stride, layout.Stride);
    private readonly WarSnakeMemoryLayout _layout = layout;

    public ref Profile GetProfile() => 
        ref MemoryMarshal.GetReference(MemoryMarshal.Cast<byte, Profile>(_memory[.._layout.ProfileSize]));

    public ref Health GetHealth() => 
        ref MemoryMarshal.GetReference(MemoryMarshal.Cast<byte, Health>(_memory.Slice(_layout.ProfileSize, _layout.HealthSize)));

    public ref Anatomy GetAnatomy() =>
        ref MemoryMarshal.GetReference(MemoryMarshal.Cast<byte, Anatomy>(_memory.Slice(_layout.ProfileSize + _layout.HealthSize, _layout.AnatomySize)));
    
    public Span<ushort> GetBody() =>
        MemoryMarshal.Cast<byte, ushort>(_memory.Slice(_layout.HeaderSize, _layout.BodySize));
    
    public ReadOnlySpan<byte> GetRawData() => _memory;
}