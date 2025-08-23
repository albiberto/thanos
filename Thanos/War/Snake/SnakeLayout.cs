using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Thanos.Memory;

namespace Thanos.War.Snake;

public readonly unsafe struct SnakeLayout
{
    public readonly int ProfileSize;
    public readonly int HealthSize;
    public readonly int AnatomySize;
    public readonly int BodySize;
        
    public readonly int HeaderSize;
    
    public readonly int Stride;

    public SnakeLayout(int capacity)
    {
        ProfileSize = sizeof(Profile);
        HealthSize = sizeof(Health);
        AnatomySize = sizeof(Anatomy);
        
        HeaderSize = (ProfileSize + HealthSize + AnatomySize).AlignUp();
        BodySize = (sizeof(ushort) * capacity).AlignUp();
        
        Stride = HeaderSize + BodySize;
    }
}


public readonly ref struct WarSnakeMemoryView(Span<byte> snakesMemory, in SnakeLayout layout, int id)
{
    private readonly Span<byte> _memory = snakesMemory.Slice(id * layout.Stride, layout.Stride);
    private readonly SnakeLayout _layout = layout;

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