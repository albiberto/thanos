using Thanos.War;
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
    // Rappresenta l'intera memoria di UN serpente.
    // Snake.Offset = 0
    // Snake.Length = Dimensione dati
    // Snake.Next   = STRIDE (Offset del prossimo serpente)
    public readonly MemoryBlock SnakeStride; 
    
    public readonly ushort QueueCapacity;

    // --- Global Blocks ---
    public readonly MemoryBlock CollisionsBitboard;
    public readonly MemoryBlock FoodBitboard;
    public readonly MemoryBlock HazardsBitboard;

    // --- Container Block: SLOT ---
    // Rappresenta l'intera memoria di UNO Slot (Arena).
    // Slot.Next = SLOT STRIDE (Offset del prossimo slot nel pool)
    public readonly MemoryBlock SlotStride;

    public SlotMemoryLayout(ushort area, ushort queueCapacity, byte maxSnakeCount)
    {
        QueueCapacity = queueCapacity;
        var ulongCount = (area + 63) / 64; 

        // 1. Definiamo i sotto-blocchi dello Snake
        WarSnakeLife = MemoryBlock.CreateUp8<WarSnakeLife>(0, 1);
        Bitboard = MemoryBlock.CreateUp8<ulong>(WarSnakeLife.Next, ulongCount);
        CircularQueueState = MemoryBlock.CreateUp8<CircularQueueState>(Bitboard.Next, 1);
        QueueBuffer = MemoryBlock.CreateUp64<ushort>(CircularQueueState.Next, queueCapacity);

        // 2. Definiamo il blocco SNAKE (Contenitore)
        // Inizia a 0.
        // La sua lunghezza "utile" finisce dove finisce il QueueBuffer.
        // Forziamo l'allineamento a 64 byte per il Next.
        // QUINDI: Snake.Next è il nostro "SnakeStride".
        SnakeStride = MemoryBlock.CreateUp64(0, QueueBuffer.Next);

        // 3. Blocchi Globali
        // Iniziano dove finisce l'array di serpenti (Snake.Next * N)
        var snakesTotalSize = SnakeStride.Next * maxSnakeCount;

        CollisionsBitboard = MemoryBlock.CreateUp64<ulong>(snakesTotalSize, ulongCount);
        FoodBitboard = MemoryBlock.CreateUp8<ulong>(CollisionsBitboard.Next, ulongCount);
        HazardsBitboard = MemoryBlock.CreateUp8<ulong>(FoodBitboard.Next, ulongCount);

        // 4. Definiamo il blocco SLOT (Contenitore Totale)
        // Inizia a 0.
        // Finisce dove finisce l'ultimo bitboard.
        // Forziamo allineamento a 64 byte.
        // QUINDI: Slot.Next è il nostro "SlotStride".
        SlotStride = MemoryBlock.CreateUp64(0, HazardsBitboard.Next);
    }
}