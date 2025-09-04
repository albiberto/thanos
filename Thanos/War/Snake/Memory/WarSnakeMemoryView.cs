using System.Runtime.InteropServices;

namespace Thanos.War.Snake.Memory;

public readonly unsafe ref struct WarSnakeMemoryView
{
    public readonly ref Health Health;
    public readonly ref Anatomy Anatomy;
    public readonly Span<ushort> Body;
    public readonly int BodyCapacity;

    public WarSnakeMemoryView(Span<byte> headersMemory, Span<byte> bodiesMemory, in WarSnakeMemoryLayout layout, int snakeId)
    {
        var headerOffset = snakeId * layout.HeaderStride;
        var headerMemory = headersMemory.Slice(headerOffset, layout.HeaderStride);
        Health = ref MemoryMarshal.AsRef<Health>(headerMemory);
        Anatomy = ref MemoryMarshal.AsRef<Anatomy>(headerMemory[sizeof(Health)..]);
        
        var bodyOffset = snakeId * layout.BodySize;
        var bodyMemory = bodiesMemory.Slice(bodyOffset, layout.BodySize);
        Body = MemoryMarshal.Cast<byte, ushort>(bodyMemory);
        
        BodyCapacity = layout.BodyCapacity;
    }
}