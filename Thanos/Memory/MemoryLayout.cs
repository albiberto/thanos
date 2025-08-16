// Thanos.Memory/MemoryLayout.cs

using System.Buffers;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Thanos.Extensions;
using Thanos.MCST;
using Thanos.War;

[StructLayout(LayoutKind.Sequential)]
public readonly struct MemoryLayout
{
    public readonly uint SnakeBodyCapacity;
    public readonly SizesLayout Sizes;
    public readonly OffsetsLayout Offsets;
    
    public MemoryLayout(in WarContext context, uint maxNodes)
    {
        SnakeBodyCapacity = Math.Min(BitOperations.RoundUpToPowerOf2(context.Area), Constants.MaxSnakeBodyCapacity);
        
        var sizeOfSnakeHeader = (uint)Unsafe.SizeOf<WarSnakeHeader>().AlignUp();
        var snakeStride = (sizeOfSnakeHeader + SnakeBodyCapacity * sizeof(ushort)).AlignUp();
        var sizeOfSnakes = snakeStride * context.SnakeCount;

        var bitboardSegments = (context.Area + 63) >> 6;
        var bitboardStrideInBytes = (uint)(bitboardSegments * sizeof(ulong)).AlignUp();
        var sizeOfBitboards = bitboardStrideInBytes * WarField.TotalBitboards;

        Sizes = new SizesLayout(snakeStride, sizeOfSnakes, bitboardStrideInBytes, sizeOfBitboards, maxNodes);
        Offsets = new OffsetsLayout(Sizes);
    }
    
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct SizesLayout
    {
        public readonly int Node = Unsafe.SizeOf<Node>().AlignUp();
        // NON CI SONO PIÙ WarArena e WarFieldHeader!
        
        public readonly int SnakeStride;
        public readonly int Snakes;
        public readonly int BitboardStride; // Dimensione in bytes
        public readonly int BitboardStrideInUlongs; // Dimensione in ulongs
        public readonly int Bitboards;
        public readonly int Slot;
        public readonly nuint Pool;

        public SizesLayout(uint snakeStride, uint sizeOfSnakes, uint bitboardStride, uint sizeOfBitboards, uint maxNodes)
        {
            SnakeStride = (int)snakeStride;
            Snakes = (int)sizeOfSnakes;
            BitboardStride = (int)bitboardStride;
            BitboardStrideInUlongs = (int)bitboardStride / sizeof(ulong);
            Bitboards = (int)sizeOfBitboards;
            // Lo slot ora contiene solo i dati reali
            Slot = Node + Bitboards + Snakes;
            Pool = (nuint)((long)Slot * maxNodes);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public readonly struct OffsetsLayout
    {
        // Schema di memoria semplificato: [NODE] [BITBOARDS] [SNAKES]
        public readonly int Node;
        public readonly int Bitboards;
        public readonly int Snakes;
        // ARENA e FIELD non hanno più un offset, perché sono viste temporanee.

        public OffsetsLayout(in SizesLayout sizes)
        {
            Node = 0;
            Bitboards = Node + sizes.Node;
            Snakes = Bitboards + sizes.Bitboards;
        }
    }
}