using System.Numerics;
using System.Runtime.InteropServices;
using Thanos.Enums;
using Thanos.Extensions;
using Thanos.MCST;
using Thanos.War;

namespace Thanos.Memory;

[StructLayout(LayoutKind.Sequential)]
public readonly struct MemoryLayout
{
    public readonly uint SnakeBodyCapacity;
    
    public readonly Size Sizes;
    public readonly Offset Offsets;
    
    public MemoryLayout(in WarContext context, uint maxNodes)
    {
        SnakeBodyCapacity = System.Math.Min(BitOperations.RoundUpToPowerOf2(context.Area), Constants.MaxSnakeBodyCapacity);
        
        var snakeStride = (Size.WarSnakeHeader + SnakeBodyCapacity * sizeof(ushort)).AlignUp();
        var sizeOfSnakes = snakeStride * context.SnakeCount;

        var bitboardSegments = (context.Area + 63) >> 6;
        var bitboardStride = (bitboardSegments * sizeof(ulong)).AlignUp();
        var sizeOfBitboards = bitboardStride * WarField.TotalBitboards;

        Offsets = new Offset(sizeOfSnakes, bitboardSegments);
        Sizes = new Size(snakeStride, sizeOfSnakes, bitboardStride, sizeOfBitboards, maxNodes);
    }

    [StructLayout(LayoutKind.Sequential)]
    public readonly unsafe struct Size
    {
        // Dimensioni allineate dei componenti principali di uno Slot.
        public static readonly uint Node = sizeof(Node).AlignUp();
        public static readonly uint WarArena = sizeof(WarArena).AlignUp();
        public static readonly uint WarSnakeHeader = sizeof(WarSnake).AlignUp();
        public static readonly uint WarFieldHeader = sizeof(WarField).AlignUp();

        // Dimensioni calcolate dinamicamente in base al contesto del gioco.
        public readonly uint SnakeStride;      // Dimensione di 1 serpente (header + corpo)
        public readonly uint Snakes;           // Dimensione totale per tutti i serpenti
        public readonly uint BitboardStride;   // Dimensione di 1 bitboard
        public readonly uint Bitboards;        // Dimensione totale per tutti i bitboard
        public readonly uint Slot;             // Dimensione totale di un MemorySlot
        public readonly nuint Pool;            // Dimensione totale di tutta la memoria

        public Size(uint snakeStride, uint sizeOfSnakes, uint bitboardStride, uint sizeOfBitboards, uint maxNodes)
        {
            SnakeStride = snakeStride;
            Snakes = sizeOfSnakes;
            BitboardStride = bitboardStride;
            Bitboards = sizeOfBitboards;
            
            // La dimensione totale di uno slot è la somma dei suoi componenti.
            Slot = Node + WarArena + WarFieldHeader + Snakes + Bitboards;
            Pool = Slot * maxNodes;
        }
    }

    // Rinominato in OffsetsLayout per coerenza
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct Offset
    {
        // Offset (in byte) dall'inizio di un MemorySlot.
        public readonly uint Node;
        public readonly uint WarArena;
        public readonly uint WarField;
        public readonly uint Snakes;
        public readonly uint Bitboards;

        // Questo non è un offset, ma una dimensione/conteggio, ma lo teniamo se ti serve qui.
        public readonly uint BitboardSegments;

        public Offset(uint sizeOfSnakes, uint bitboardSegments)
        {
            // Gli offset sono calcolati sequenzialmente.
            Node = 0;
            WarArena = Node + Size.Node;
            WarField = WarArena + Size.WarArena;
            Snakes = WarField + Size.WarFieldHeader;
            Bitboards = Snakes + sizeOfSnakes;
            
            BitboardSegments = bitboardSegments;
        }
    }
}