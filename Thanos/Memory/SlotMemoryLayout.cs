using Thanos.War;
using Thanos.War.Snake;
using Thanos.War.Structures;

namespace Thanos.Memory;

public readonly struct SlotMemoryLayout
{
    // --- Snake Local Blocks ---
    public readonly MemoryBlock WarSnakeLife;
    public readonly MemoryBlock Bitboard;
    public readonly MemoryBlock CircularQueueState;
    public readonly MemoryBlock QueueBuffer;

    // --- Container Block: SNAKE ---
    public readonly MemoryBlock SnakeStride; 
    
    public readonly ushort QueueCapacity;

    // --- Global Blocks ---
    public readonly MemoryBlock CollisionsBitboard;
    public readonly MemoryBlock FoodBitboard;
    public readonly MemoryBlock HazardsBitboard;

    // --- Container Block: SLOT ---
    public readonly MemoryBlock SlotStride;

    public SlotMemoryLayout(ushort area, ushort queueCapacity, byte maxSnakeCount)
    {
        QueueCapacity = queueCapacity;
        // Per 11x11 (121 bit), (121 + 63) / 64 = 2 ulongs.
        var ulongCount = (area + 63) / 64; 

        // 1. WarSnakeLife -> Allineamento 8
        WarSnakeLife = MemoryBlock.CreateUp8<WarSnakeLife>(0, 1);
        
        // 2. Bitboard -> Allineamento 16 (CRITICO PER SIMD/NEON)
        // Inserirà padding automatico dopo WarSnakeLife
        Bitboard = MemoryBlock.CreateUp16<ulong>(WarSnakeLife.Next, ulongCount);
        
        // 3. Queue State -> Allineamento 8
        CircularQueueState = MemoryBlock.CreateUp8<CircularQueueState>(Bitboard.Next, 1);
        
        // 4. Queue Buffer -> Allineamento 64 (Inizio Cache Line pulita per evitare false sharing)
        QueueBuffer = MemoryBlock.CreateUp64<ushort>(CircularQueueState.Next, queueCapacity);

        // 5. Snake Stride -> Allineamento 64 (Contenitore Snake)
        SnakeStride = MemoryBlock.CreateUp64(0, QueueBuffer.Next);

        // --- Global Blocks ---
        var snakesTotalSize = SnakeStride.Next * maxSnakeCount;

        // Le Bitboard globali le allineiamo a 32 byte (Future-proof per AVX)
        // Va bene anche per NEON (richiede 16, 32 è multiplo di 16).
        CollisionsBitboard = MemoryBlock.CreateUp32<ulong>(snakesTotalSize, ulongCount);
        FoodBitboard = MemoryBlock.CreateUp32<ulong>(CollisionsBitboard.Next, ulongCount);
        HazardsBitboard = MemoryBlock.CreateUp32<ulong>(FoodBitboard.Next, ulongCount);

        // Slot Stride -> Allineamento 64
        SlotStride = MemoryBlock.CreateUp64(0, HazardsBitboard.Next);
    }
}