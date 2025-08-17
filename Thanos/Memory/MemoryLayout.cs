using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Thanos.Enums;
using Thanos.MCST;
using Thanos.War;

namespace Thanos.Memory;

[StructLayout(LayoutKind.Sequential)]
public readonly struct MemoryLayout
{
    // --- Dimensioni ---
    public readonly int NodeSize;
    public readonly int BitboardsSize;
    public readonly int SnakesSize;
    public readonly int SnakeStride;
    public readonly int BitboardStrideInBytes;
    public readonly int BitboardStrideInUlongs;

    // --- Offset ---
    public readonly int NodeOffset;
    public readonly int BitboardsOffset;
    public readonly int SnakesOffset;

    // --- Totali ---
    public readonly int SlotSize;
    public readonly long PoolSize; // Usiamo long per la massima sicurezza

    public MemoryLayout(in WarContext context, int maxNodes) // Accetta int
    {
        // --- 1. Calcolo Dimensioni ---
        NodeSize = Unsafe.SizeOf<Node>().AlignUp();

        // I calcoli ora usano 'int'
        var bitboardSegments = (context.Area + 63) >> 6;
        BitboardStrideInBytes = (bitboardSegments * sizeof(ulong)).AlignUp();
        BitboardStrideInUlongs = BitboardStrideInBytes / sizeof(ulong);
        BitboardsSize = BitboardStrideInBytes * WarField.TotalBitboards;

        // La capacità del corpo è ora 'int', ma RoundUpToPowerOf2 richiede 'uint'.
        // Questa è una delle poche conversioni esplicite e necessarie.
        var snakeBodyCapacity = (int)Math.Min(BitOperations.RoundUpToPowerOf2((uint)context.Area), Constants.MaxSnakeBodyCapacity);
        
        var snakeHeaderSize = Unsafe.SizeOf<WarSnakeHeader>().AlignUp();
        SnakeStride = (snakeHeaderSize + snakeBodyCapacity * sizeof(ushort)).AlignUp();
        SnakesSize = SnakeStride * context.SnakeCount;

        // --- 2. Calcolo Totali ---
        SlotSize = NodeSize + BitboardsSize + SnakesSize;
        // Moltiplichiamo come 'long' per evitare overflow con 'maxNodes' molto grandi
        PoolSize = (long)SlotSize * maxNodes;

        // --- 3. Calcolo Offset ---
        NodeOffset = 0;
        BitboardsOffset = NodeOffset + NodeSize;
        SnakesOffset = BitboardsOffset + BitboardsSize;
    }
}

public static class MemoryExtensions
{
    private const long Alignment = Constants.SizeOfCacheLine;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int AlignUp(this int value) => (int)((value + Alignment - 1) & ~(Alignment - 1));
}
