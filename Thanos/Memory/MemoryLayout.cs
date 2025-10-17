namespace Thanos.Memory;

public class MemoryLayout(
    int slotSize,
    int headerStride,
    int bitboardSize,
    int headersBaseOffset,
    int[] bitboardOffsets,
    int circularBuffersBaseOffset,
    int circularBufferStride,
    int capacity)
{
    public readonly int SlotSize = slotSize;
    public readonly int HeaderStride = headerStride;
    public readonly int BitboardSize = bitboardSize;
    public readonly int HeadersBaseOffset = headersBaseOffset;
    public readonly int[] BitboardOffsets = bitboardOffsets;
    
    public readonly int CircularBuffersBaseOffset = circularBuffersBaseOffset;
    public readonly int CircularBufferStride = circularBufferStride;
    public readonly int Capacity = capacity;

    // SCORCIATOIE RIPRISTINATE: Leggono dall'array di offset
    public int FoodBitboardOffset => BitboardOffsets[0];
    public int HazardsBitboardOffset => BitboardOffsets[1];
    public int SnakesBitboardOffset => BitboardOffsets[2];
}