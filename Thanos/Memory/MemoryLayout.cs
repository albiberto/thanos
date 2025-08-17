using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Thanos.Enums;
using Thanos.MCST;
using Thanos.War;

namespace Thanos.Memory;

[StructLayout(LayoutKind.Sequential)]
public readonly unsafe struct MemoryLayout
{
    // --- Dimensioni ---
    public readonly int NodeSize;
    public readonly int BitboardsSize;
    public readonly int SnakesSize;
    public readonly int SnakeStride;
    public readonly int BitboardStrideInBytes;
    public readonly int BitboardStrideInUlongs;
    public readonly int WarArenaHeaderSize;

    // --- Offset ---
    public readonly int NodeOffset;
    public readonly int BitboardsOffset;
    public readonly int SnakesOffset;
    public readonly int WarArenaHeaderOffset;

    // --- Totali ---
    public readonly int SlotSize;
    public readonly long PoolSize;

    public MemoryLayout(in WarContext context, int maxNodes)
    {
        // --- 1. Calcolo Dimensioni ---
        NodeSize = sizeof(Node).AlignUp();

        var bitboardSegments = (context.Area + 63) >> 6;
        BitboardStrideInBytes = (bitboardSegments * sizeof(ulong)).AlignUp();
        BitboardStrideInUlongs = BitboardStrideInBytes / sizeof(ulong);
        BitboardsSize = BitboardStrideInBytes * WarField.TotalBitboards;

        var snakeBodyCapacity = (int)Math.Min(BitOperations.RoundUpToPowerOf2((uint)context.Area), Constants.MaxSnakeBodyCapacity);
        var snakeHeaderSize = sizeof(WarSnakeHeader).AlignUp();
        SnakeStride = (snakeHeaderSize + snakeBodyCapacity * sizeof(ushort)).AlignUp();
        SnakesSize = SnakeStride * context.InitialSnakeCount;
        
        WarArenaHeaderSize = sizeof(WarArenaHeader).AlignUp();

        // --- 2. Calcolo Totali ---
        SlotSize = NodeSize + BitboardsSize + SnakesSize + WarArenaHeaderSize;
        PoolSize = (long)SlotSize * maxNodes;

        // --- 3. Calcolo Offset ---
        NodeOffset = 0;
        BitboardsOffset = NodeOffset + NodeSize;
        SnakesOffset = BitboardsOffset + BitboardsSize;
        WarArenaHeaderOffset = SnakesOffset + SnakesSize;
    }
}

public static class MemoryExtensions
{
    private const long Alignment = Constants.SizeOfCacheLine;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int AlignUp(this int value) => (int)((value + Alignment - 1) & ~(Alignment - 1));
}
