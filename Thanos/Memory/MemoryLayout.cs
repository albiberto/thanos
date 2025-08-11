using System.Runtime.InteropServices;
using Thanos.Extensions;
using Thanos.MCST;
using Thanos.War;

namespace Thanos.Memory;

[StructLayout(LayoutKind.Sequential)]
public readonly unsafe struct MemoryLayout
{
    public readonly uint SizeOfContext, SizeOfLayout, SizeOfArena, SizeOfNode;
    public readonly uint SnakeStride, SnakeBodyStride, SizeOfSnakes;
    public readonly uint BitboardStride, SizeOfBitboards, SizeOfFiled;

    public readonly nuint SizeOfMemorySlot, SizeOfMemoryPool;
    
    public readonly long ContextOffset, LayoutOffset, NodeOffset, ArenaOffset, FieldOffset, SnakesOffset, SnakeBodyOffset, BitboardsOffset;
    public readonly nuint TotalSlotSize, TotalMemorySize;
    
    public MemoryLayout(in WarContext context, uint maxNodes)
    {
        // Calculate the pointer's offsets.
        ContextOffset = 0;
        LayoutOffset = ContextOffset + SizeOfContext;
        
        NodeOffset = LayoutOffset + SizeOfLayout;
        ArenaOffset = NodeOffset + SizeOfNode;
        FieldOffset = ArenaOffset + SizeOfArena;
        SnakesOffset = FieldOffset + SizeOfFiled;
    }
}