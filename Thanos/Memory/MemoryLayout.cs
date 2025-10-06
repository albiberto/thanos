using System.Runtime.CompilerServices;
using Thanos.Common;
using Thanos.War;

namespace Thanos.Memory;

public readonly unsafe struct MemoryLayout(int slotSize, int headerStride, int bitboardSize, int headersBaseOffset, int[] bitboardOffsets)
{
    // NUOVO: Costante pubblica per sapere sempre dove si trova il nostro dato.
    // Il builder lo piazzerà sempre a offset 0.
    public const int PlayerToMoveIndexOffset = 0;

    public int SizeOfHeader => sizeof(WarSnakeHeader);

    public int SlotSize { get; } = slotSize.AlignUp64();
    public int HeaderStride { get; } = headerStride;
    public int BitboardSize { get; } = bitboardSize;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetSnakeHeaderOffset(int index) => headersBaseOffset + index * HeaderStride;

    public readonly int FoodBitboardOffset = bitboardOffsets[0];
    public readonly int HazardsBitboardOffset = bitboardOffsets[1];
    public readonly int SnakesBitboardOffset = bitboardOffsets[2];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetSnakeBitboardOffset(int snakeIndex) => bitboardOffsets[LayoutConstants.GlobalBitboardCount + snakeIndex];
}