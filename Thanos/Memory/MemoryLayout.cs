using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Thanos.Enums;
using Thanos.Extensions; // Per il metodo AlignUp()
using Thanos.MCST;
using Thanos.War;

namespace Thanos.Memory;

/// <summary>
/// Calcola e memorizza le dimensioni e gli offset per un singolo blocco di memoria (Slot)
/// basandosi sul contesto di gioco. Questa struct è immutabile e contiene tutte le informazioni
/// necessarie per partizionare correttamente la memoria.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public readonly struct MemoryLayout
{
    // --- Dimensioni dei Componenti ---
    public readonly int NodeSize;
    public readonly int BitboardsSize;
    public readonly int SnakesSize;
    public readonly int SnakeStride;
    public readonly int BitboardStrideInBytes;
    public readonly int BitboardStrideInUlongs;

    // --- Offset dei Componenti ---
    public readonly int NodeOffset;
    public readonly int BitboardsOffset;
    public readonly int SnakesOffset;

    // --- Dimensioni Totali ---
    public readonly int SlotSize;
    public readonly int PoolSize;

    public MemoryLayout(in WarContext context, uint maxNodes)
    {
        // --- 1. Calcolo Dimensioni dei Blocchi ---

        NodeSize = Unsafe.SizeOf<Node>().AlignUp();

        // Calcolo per i Bitboards
        var bitboardSegments = (context.Area + 63) >> 6;
        BitboardStrideInBytes = (int)(bitboardSegments * sizeof(ulong)).AlignUp();
        BitboardStrideInUlongs = BitboardStrideInBytes / sizeof(ulong);
        BitboardsSize = BitboardStrideInBytes * WarField.TotalBitboards;

        // Calcolo per i Serpenti
        var snakeBodyCapacity = Math.Min(BitOperations.RoundUpToPowerOf2(context.Area), Constants.MaxSnakeBodyCapacity);
        var snakeHeaderSize = Unsafe.SizeOf<WarSnakeHeader>().AlignUp();
        SnakeStride = (int)(snakeHeaderSize + snakeBodyCapacity * sizeof(ushort)).AlignUp();
        SnakesSize = SnakeStride * context.SnakeCount;

        // --- 2. Calcolo Dimensioni Totali ---
        
        // Lo Slot contiene solo i dati reali e persistenti
        SlotSize = NodeSize + BitboardsSize + SnakesSize;
        PoolSize = (int)(SlotSize * maxNodes);

        // --- 3. Calcolo Offset ---
        
        // Lo schema di memoria è [NODE] [BITBOARDS] [SNAKES]
        NodeOffset = 0;
        BitboardsOffset = NodeOffset + NodeSize;
        SnakesOffset = BitboardsOffset + BitboardsSize;
    }
}