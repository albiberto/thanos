using Thanos.War.Snake;

namespace Thanos.Memory.Pools;

public readonly unsafe struct MemoryLayout(int slotSize, int headerStride, int bitboardSize, int headersBaseOffset, int[] bitboardOffsets)
{
    public int SizeOfHealth => sizeof(Health);
    public int SizeOfAnatomy => sizeof(Anatomy);
    
    public int SlotSize { get; } = slotSize;
    public int HeaderStride { get; } = headerStride;
    public int BitboardSize { get; } = bitboardSize;

    public int GetSnakeHeaderOffset(int snakeIndex) => headersBaseOffset + snakeIndex * HeaderStride;
    
    public int GetFoodBitboardOffset() => bitboardOffsets[0];
    public int GetHazardsBitboardOffset() => bitboardOffsets[1];
    public int GetSnakeBitboardOffset(int index) => bitboardOffsets[LayoutConstants.GlobalBitboardCount + index];
}