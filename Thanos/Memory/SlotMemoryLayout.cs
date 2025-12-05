using System.Runtime.CompilerServices;
using Thanos.Common;
using Thanos.War;
using Thanos.War.Structures;

namespace Thanos.Memory;

public readonly struct SlotMemoryLayout
{
    public readonly MemoryBlock WarSnakeLife;
    public readonly MemoryBlock Bitboard;
    public readonly MemoryBlock CircularQueueState;
    public readonly MemoryBlock QueueBuffer;

    public readonly MemoryBlock SnakesBitboard;
    public readonly MemoryBlock FoodBitboard;
    public readonly MemoryBlock HazardsBitboard;

    public readonly int SnakeStride;
    public readonly int SlotSize;
    public readonly ushort QueueCapacity;

    public static SlotMemoryLayout Medium => new(Constants.Medium.Area, 128, Constants.MaxSnakesCount);

    private SlotMemoryLayout(int area, ushort queueCapacity, int maxSnakeCount)
    {
        QueueCapacity = queueCapacity;
        
        var bitboardByteSize = sizeof(ulong) * ((area + 63) / 64);

        var relOffset = 0;

        WarSnakeLife = new MemoryBlock(relOffset, 1); 
        relOffset += Unsafe.SizeOf<WarSnakeLife>();

        Bitboard = new MemoryBlock(relOffset, bitboardByteSize);
        relOffset += bitboardByteSize;

        // 3. Queue State (piccolo, ~4-8 byte)
        CircularQueueState = new MemoryBlock(relOffset, 1);
        relOffset += Unsafe.SizeOf<CircularQueueState>();

        // 4. Queue Buffer (Heavy Data)
        // OTTIMIZZAZIONE CACHE:
        // Allineiamo il buffer della coda a 64 byte. 
        // Questo spreca qualche byte (gap) tra lo State e il Buffer, 
        // ma garantisce che quando scorriamo l'array del corpo, partiamo da una cache line fresca.
        relOffset = relOffset.AlignUp64();
        
        QueueBuffer = new MemoryBlock(relOffset, queueCapacity);
        relOffset += sizeof(ushort) * queueCapacity;

        // 5. Chiusura Stride
        // Allineiamo la fine del serpente a 64 byte. 
        // Così il serpente successivo inizia pulito su una nuova cache line.
        SnakeStride = relOffset.AlignUp64();

        // --- LAYOUT GLOBALE SLOT ---
        var globalOffset = SnakeStride * maxSnakeCount;

        // I Bitboard condivisi dovrebbero essere allineati per operazioni SIMD/Bitmask veloci
        globalOffset = globalOffset.AlignUp64();

        SnakesBitboard = new MemoryBlock(globalOffset, bitboardByteSize);
        globalOffset += bitboardByteSize;

        FoodBitboard = new MemoryBlock(globalOffset, bitboardByteSize);
        globalOffset += bitboardByteSize;

        HazardsBitboard = new MemoryBlock(globalOffset, bitboardByteSize);
        globalOffset += bitboardByteSize;

        // Dimensione finale Slot
        SlotSize = globalOffset.AlignUp64();
    }
}