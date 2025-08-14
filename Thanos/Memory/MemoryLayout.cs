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
        public static readonly uint Node = sizeof(Node).AlignUp();
        public static readonly uint WarArena = sizeof(WarArena).AlignUp();
        public static readonly uint WarSnakeHeader = sizeof(WarSnake).AlignUp(); // Dimensione del blocco header, non della struct
        public static readonly uint WarFieldHeader = sizeof(WarField).AlignUp();

        public readonly uint SnakeStride;
        public readonly uint Snakes;
        public readonly uint BitboardStride;
        public readonly uint Bitboards;
        public readonly uint Slot;
        public readonly ulong Pool;

        public SizesLayout(uint snakeStride, uint sizeOfSnakes, uint bitboardStride, uint sizeOfBitboards, uint maxNodes)
        {
            SnakeStride = snakeStride;
            Snakes = sizeOfSnakes;
            BitboardStride = bitboardStride;
            Bitboards = sizeOfBitboards;
            Slot = Node + WarArena + WarFieldHeader + Snakes + Bitboards;
            Pool = (ulong)Slot * maxNodes;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public readonly struct OffsetsLayout
    {
        public readonly uint Node;
        public readonly uint Snakes;
        public readonly uint Bitboards;
        public readonly uint WarField;
        public readonly uint WarArena;

        public readonly uint BitboardSegments;

        // Schema: NODE | SNAKES | BITBOARDS | WARFIELD | ARENA
        public OffsetsLayout(uint sizeOfSnakes, uint sizeOfBitboards, uint bitboardSegments)
        {
            Node = 0;
            Snakes = Node + SizesLayout.Node;
            Bitboards = Snakes + sizeOfSnakes;
            WarField = Bitboards + sizeOfBitboards;
            WarArena = WarField + SizesLayout.WarFieldHeader;
            
            BitboardSegments = bitboardSegments;
        }
    }
}