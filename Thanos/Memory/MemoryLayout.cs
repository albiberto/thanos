using System.Numerics;
using System.Runtime.InteropServices;
using Thanos.Enums;
using Thanos.Extensions; // Assumo che AlignUp() sia qui
using Thanos.MCST;
using Thanos.War;

namespace Thanos.Memory;

[StructLayout(LayoutKind.Sequential)]
public readonly struct MemoryLayout
{
    public readonly uint SnakeBodyCapacity;
    
    public readonly SizesLayout Sizes;
    public readonly OffsetsLayout Offsets;
    
    public MemoryLayout(in WarContext context, uint maxNodes)
    {
        // Calcolo della capacità del corpo del serpente, arrotondata alla potenza di 2 più vicina.
        SnakeBodyCapacity = System.Math.Min(BitOperations.RoundUpToPowerOf2(context.Area), Constants.MaxSnakeBodyCapacity);
        
        // Calcolo delle dimensioni principali, con allineamento per performance.
        var snakeStride = (SizesLayout.WarSnakeHeader + SnakeBodyCapacity * sizeof(ushort)).AlignUp();
        var sizeOfSnakes = (snakeStride * context.SnakeCount).AlignUp();

        var bitboardSegments = (context.Area + 63) >> 6;
        var bitboardStride = bitboardSegments * sizeof(ulong);
        var sizeOfBitboards = (bitboardStride * WarField.TotalBitboards).AlignUp();

        // Inizializzazione delle struct di layout.
        Offsets = new OffsetsLayout(sizeOfSnakes, bitboardSegments);
        Sizes = new SizesLayout(snakeStride, sizeOfSnakes, bitboardStride, sizeOfBitboards, maxNodes);
    }

    // Rinominato in SizesLayout per evitare conflitto di nomi con System.Drawing.Size
    [StructLayout(LayoutKind.Sequential)]
    public readonly unsafe struct SizesLayout
    {
        // Dimensioni allineate dei componenti principali di uno Slot.
        public static readonly uint Node = (uint)sizeof(Node).AlignUp();
        public static readonly uint WarArena = (uint)sizeof(WarArena).AlignUp();
        public static readonly uint WarSnakeHeader = (uint)sizeof(WarSnake).AlignUp();
        public static readonly uint WarFieldHeader = (uint)sizeof(WarField).AlignUp();

        // Dimensioni calcolate dinamicamente in base al contesto del gioco.
        public readonly uint SnakeStride;      // Dimensione di 1 serpente (header + corpo)
        public readonly uint Snakes;           // Dimensione totale per tutti i serpenti
        public readonly uint BitboardStride;   // Dimensione di 1 bitboard
        public readonly uint Bitboards;        // Dimensione totale per tutti i bitboard
        public readonly uint Slot;             // Dimensione totale di un MemorySlot
        public readonly nuint Pool;            // Dimensione totale di tutta la memoria

        public SizesLayout(uint snakeStride, uint sizeOfSnakes, uint bitboardStride, uint sizeOfBitboards, uint maxNodes)
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
    public readonly struct OffsetsLayout
    {
        // Offset (in byte) dall'inizio di un MemorySlot.
        public readonly uint Node;
        public readonly uint WarArena;
        public readonly uint WarField;
        public readonly uint Snakes;
        public readonly uint Bitboards;

        // Questo non è un offset, ma una dimensione/conteggio, ma lo teniamo se ti serve qui.
        public readonly uint BitboardSegments;

        public OffsetsLayout(uint sizeOfSnakes, uint bitboardSegments)
        {
            // Gli offset sono calcolati sequenzialmente.
            Node = 0;
            WarArena = Node + SizesLayout.Node;
            WarField = WarArena + SizesLayout.WarArena;
            Snakes = WarField + SizesLayout.WarFieldHeader;
            Bitboards = Snakes + sizeOfSnakes;
            
            BitboardSegments = bitboardSegments;
        }
    }
}