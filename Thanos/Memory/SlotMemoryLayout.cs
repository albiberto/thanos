using System.Runtime.CompilerServices;
using Thanos.Common;
using Thanos.War;
using Thanos.War.Structures;

namespace Thanos.Memory;

public class SlotMemoryLayout
{
    // --- Dimensioni Componenti ---
    public readonly int WarSnakeLifeSize;
    public readonly int CircularQueueStateSize;
    public readonly int BitboardSize;
    public readonly int QueueBufferSize;
    public readonly int SnakeStride;

    // --- Offset Relativi ---
    public readonly int WarSnakeLifeOffset;
    public readonly int CircularQueueStateOffset;
    public readonly int BitboardOffset;
    public readonly int QueueBufferOffset;

    public SlotMemoryLayout(int area, int capacity)
    {
        // 1. CALCOLA DIMENSIONI COMPONENTI
        WarSnakeLifeSize = Unsafe.SizeOf<WarSnakeLife>();               //      4 bytes
        BitboardSize = sizeof(ulong) * ((area + 63) / 64);              //  +  16 bytes
        CircularQueueStateSize = Unsafe.SizeOf<CircularQueueState>();   //  +   6 bytes
        QueueBufferSize = sizeof(ushort) * capacity;                    //  + 256 bytes
                                                                        //  -----------
                                                                        //  = 282 bytes (Dati)
        // 2. CALCOLA LAYOUT RELATIVO DI UN SINGOLO SERPENTE
        var relOffset = 0;
        
        WarSnakeLifeOffset = relOffset; 
        relOffset += WarSnakeLifeSize;                                  // 0 +  4   =  4
        
        BitboardOffset = relOffset;
        relOffset += BitboardSize;                                      // 4 + 16   = 20
        
        CircularQueueStateOffset = relOffset;
        relOffset += CircularQueueStateSize;                            // 20 + 6   = 26

        relOffset = relOffset.AlignUp64();                              // 26       > 64
        QueueBufferOffset = relOffset;
        
        relOffset += QueueBufferSize;                                   // 64 + 256 = 320
        SnakeStride = relOffset;
    }
}