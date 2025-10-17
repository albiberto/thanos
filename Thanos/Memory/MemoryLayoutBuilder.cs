using System.Numerics;
using Thanos.Common;
using Thanos.War;

namespace Thanos.Memory;

public unsafe class MemoryLayoutBuilder(int area, int snakeCount)
{
    public static MemoryLayout Worst => new MemoryLayoutBuilder(Constants.Large, Constants.MaxSnakesCount).Build();

    public MemoryLayout Build()
    {
        // 1. Calcolo Capacità
        var requiredSize = (uint)(area + 1);
        var nextPowerOfTwo = BitOperations.RoundUpToPowerOf2(requiredSize);
        var capacity = (int)Math.Min(256, nextPowerOfTwo);
        
        // 2. Sezione Header
        const int playerToMoveIndexSize = sizeof(int);
        var headerStride = sizeof(WarSnakeHeader);
        var headersTotalSize = (headerStride * snakeCount).AlignUp64();
        var headersBaseOffset = playerToMoveIndexSize;

        // 3. Sezione Bitboard
        var ulongsNeeded = (area + 63) / 64;
        var bitboardSize = ulongsNeeded * sizeof(ulong);
        var totalBitboards = LayoutConstants.GlobalBitboardCount + snakeCount;
        var bitboardOffsets = new int[totalBitboards];
        var bitboardsBaseOffset = headersBaseOffset + headersTotalSize;
        var currentInternalOffset = 0;
        var offsetIndex = 0;

        bitboardOffsets[offsetIndex++] = AddBitboardOffset(); // Food (indice 0)
        bitboardOffsets[offsetIndex++] = AddBitboardOffset(); // Hazards (indice 1)
        bitboardOffsets[offsetIndex++] = AddBitboardOffset(); // AllSnakes (indice 2)
        for (var i = 0; i < snakeCount; i++) bitboardOffsets[offsetIndex++] = AddBitboardOffset();
        var bitboardsTotalSize = currentInternalOffset;

        // 4. Sezione Buffer Circolari
        var circularBufferStride = (capacity * sizeof(ushort)).AlignUp64();
        var circularBuffersTotalSize = circularBufferStride * snakeCount;
        var circularBuffersBaseOffset = (bitboardsBaseOffset + bitboardsTotalSize).AlignUp64();

        // 5. Calcolo Finale
        var slotSize = (circularBuffersBaseOffset + circularBuffersTotalSize).AlignUp64();

        return new MemoryLayout(
            slotSize, 
            headerStride, 
            bitboardSize, 
            headersBaseOffset, 
            bitboardOffsets, // L'array viene passato al costruttore
            circularBuffersBaseOffset,
            circularBufferStride,
            capacity
        );

        int AddBitboardOffset()
        {
            var startCacheLine = currentInternalOffset / Constants.CacheLine;
            var endCacheLine = (currentInternalOffset + bitboardSize - 1) / Constants.CacheLine;
            if (startCacheLine != endCacheLine) currentInternalOffset = currentInternalOffset.AlignUp64();
            var absoluteOffset = bitboardsBaseOffset + currentInternalOffset;
            currentInternalOffset += bitboardSize;
            return absoluteOffset;
        }
    }
}