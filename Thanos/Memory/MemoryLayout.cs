using System.Runtime.CompilerServices;
using Thanos.Memory.Pools;
using Thanos.War;

namespace Thanos.Memory;

public readonly unsafe struct MemoryLayout(int slotSize, int headerStride, int bitboardSize, int headersBaseOffset, int[] bitboardOffsets)
{
    public int SizeOfHealth => sizeof(SnakeHealth);
    public int SizeOfAnatomy => sizeof(SnakeAnatomy);
    
    public int SlotSize { get; } = slotSize;
    public int HeaderStride { get; } = headerStride;
    public int BitboardSize { get; } = bitboardSize;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetSnakeHeaderOffset(int index) => headersBaseOffset + index * HeaderStride;
    
    public readonly int FoodBitboardOffset = bitboardOffsets[0];
    public readonly int HazardsBitboardOffset = bitboardOffsets[1];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetSnakeBitboardOffset(int snakeIndex) => bitboardOffsets[LayoutConstants.GlobalBitboardCount + snakeIndex];
}