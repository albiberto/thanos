using System.Runtime.CompilerServices;
using Thanos.Common;
using Thanos.War;
using Thanos.War.Structures;

namespace Thanos.Memory;

public struct SlotMemoryLayout
{
    // --- Dimensioni Componenti ---
    public readonly int WarSnakeLifeSize;
    public readonly int CircularQueueStateSize;
    public readonly int BitboardSize;
    public readonly int QueueBufferSize;

    // --- Layout per 1 Serpente ---
    public readonly int WarSnakeLifeOffset;     // Relativo all'inizio del blocco di un serpente
    public readonly int BitboardOffset;         // Relativo all'inizio del blocco di un serpente
    public readonly int CircularQueueStateOffset; // Relativo all'inizio del blocco di un serpente
    public readonly int QueueBufferOffset;      // Relativo all'inizio del blocco di un serpente
    public readonly int SnakeStride;            // Dimensione totale per 1 serpente (allineata)

    // --- Layout Globale (per Slot) ---
    public readonly int SnakesBitboardOffset;   // Offset assoluto (dall'inizio dello slot)
    public readonly int FoodBitboardOffset;     // Offset assoluto
    public readonly int HazardsBitboardOffset;  // Offset assoluto
    public readonly int SlotSize;               // Dimensione totale di 1 slot (allineata)

    public readonly ushort Capacity;

    public static SlotMemoryLayout Worst { get; } = new(area: 121, capacity: 128, snakeCount: 4);
    
    public SlotMemoryLayout(int area, ushort capacity, int snakeCount)
    {
        Capacity = capacity;
        
        WarSnakeLifeSize = Unsafe.SizeOf<WarSnakeLife>();                   // 2 bytes
        BitboardSize = sizeof(ulong) * ((area + 63) / 64);                  // 16 bytes (per area=121)
        CircularQueueStateSize = Unsafe.SizeOf<CircularQueueState>();       // 8 bytes
        QueueBufferSize = sizeof(ushort) * capacity;                        // 256 bytes (per cap=128)

        var relOffset = 0;
        WarSnakeLifeOffset = relOffset; 
        relOffset += WarSnakeLifeSize;                                      // Offset 2
        
        BitboardOffset = relOffset;
        relOffset += BitboardSize;                                          // Offset 18
        
        CircularQueueStateOffset = relOffset;
        relOffset += CircularQueueStateSize;                                // Offset 26

        relOffset = relOffset.AlignUp64();                                  // Offset 64 (allineato)
        QueueBufferOffset = relOffset;
        
        relOffset += QueueBufferSize;                                       // Offset 320 (64 + 256)
        SnakeStride = relOffset;

        var globalOffset = SnakeStride * snakeCount;                     // Es: 320 * 4 = 1280 bytes
        
        globalOffset = globalOffset.AlignUp64();

        SnakesBitboardOffset = globalOffset;
        globalOffset += BitboardSize;                                       // Offset 1296 (1280 + 16)
        
        FoodBitboardOffset = globalOffset;
        globalOffset += BitboardSize;                                       // Offset 1312 (1296 + 16)
        
        HazardsBitboardOffset = globalOffset;
        globalOffset += BitboardSize;                                       // Offset 1328 (1312 + 16)

        SlotSize = globalOffset.AlignUp64() ;                               // 1328 -> 1344 bytes
    }
}