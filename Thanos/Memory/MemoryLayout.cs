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
    
    public readonly SizesLayout Sizes;
    public readonly OffsetsLayout Offsets;
    
    public MemoryLayout(in WarContext context, uint maxNodes)
    {
        SnakeBodyCapacity = System.Math.Min(BitOperations.RoundUpToPowerOf2(context.Area), Constants.MaxSnakeBodyCapacity);
        
        // I calcoli delle dimensioni rimangono invariati
        var snakeStride = (SizesLayout.WarSnakeHeader + SnakeBodyCapacity * sizeof(ushort)).AlignUp();
        var sizeOfSnakes = snakeStride * context.SnakeCount;

        var bitboardSegments = (context.Area + 63) >> 6;
        var bitboardStride = (bitboardSegments * sizeof(ulong)).AlignUp();
        var sizeOfBitboards = bitboardStride * WarField.TotalBitboards;

        // Passiamo sizeOfBitboards al costruttore di OffsetsLayout per il nuovo calcolo
        Offsets = new OffsetsLayout(sizeOfSnakes, sizeOfBitboards, bitboardSegments);
        Sizes = new SizesLayout(snakeStride, sizeOfSnakes, bitboardStride, sizeOfBitboards, maxNodes);
    }
    
    // La struct SizesLayout non necessita di modifiche
    [StructLayout(LayoutKind.Sequential)]
    public readonly unsafe struct SizesLayout
    {
        public readonly int Node = sizeof(Node).AlignUp();
        public readonly int WarArena = sizeof(WarArena).AlignUp();
        public readonly int WarSnakeHeader = sizeof(WarSnake).AlignUp(); // Dimensione del blocco header, non della struct
        public readonly int WarFieldHeader = sizeof(WarField).AlignUp();

        public readonly int SnakeStride;
        public readonly int Snakes;
        public readonly int BitboardStride;
        public readonly int Bitboards;
        public readonly int Slot;
        public readonly nuint Pool;

        public SizesLayout(uint snakeStride, uint sizeOfSnakes, uint bitboardStride, uint sizeOfBitboards, uint maxNodes)
        {
            SnakeStride = snakeStride;
            Snakes = sizeOfSnakes;
            BitboardStride = bitboardStride;
            Bitboards = sizeOfBitboards;
            Slot = (nint)(Node + WarArena + WarFieldHeader + Snakes + Bitboards);
            Pool = (nint)(Slot * maxNodes);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public readonly struct OffsetsLayout
    {
        public readonly int Node;
        public readonly int Snakes;
        public readonly int Bitboards;
        public readonly int WarField;
        public readonly int WarArena;

        public readonly uint BitboardSegments;

        // Schema: NODE | BITBOARDS | WARFIELD | SNAKES | ARENA
        public OffsetsLayout(uint sizeOfSnakes, uint sizeOfBitboards, uint bitboardSegments)
        {
            Node = 0;
            WarField = Node + SizesLayout.Node;
            Bitboards = WarField + SizesLayout.WarFieldHeader;
            Snakes = Bitboards + sizeOfBitboards;
            WarArena = Snakes + sizeOfSnakes;
            
            BitboardSegments = bitboardSegments;
        }
    }
}