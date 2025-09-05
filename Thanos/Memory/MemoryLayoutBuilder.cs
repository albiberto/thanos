using Thanos.Common;
using Thanos.War;

namespace Thanos.Memory;

public unsafe class MemoryLayoutBuilder
{
    private int _area;
    private int _snakeCount;

    public MemoryLayoutBuilder WithGridArea(int area)
    {
        _area = area;
        return this;
    }

    public MemoryLayoutBuilder WithSnakeCount(int snakeCount)
    {
        _snakeCount = snakeCount;
        return this;
    }
    
    public MemoryLayout Build()
    {
        // 1. Calcolo Blocco Headers (invariato e corretto)
        var sizeOfHealth = sizeof(SnakeHealth);
        var sizeOfAnatomy = sizeof(SnakeAnatomy);
        var headerStride = sizeOfHealth + sizeOfAnatomy;
        var headersTotalSize = (headerStride * _snakeCount).AlignUp64();
        var headersBaseOffset = 0;

        // 2. Calcolo Blocco Bitboards
        var ulongsNeeded = (_area + 63) / 64;
        var bitboardSize = ulongsNeeded * sizeof(ulong);

        var totalBitboards = LayoutConstants.GlobalBitboardCount + _snakeCount;
        var bitboardOffsets = new int[totalBitboards];
    
        // Questo è l'offset di partenza del blocco dei bitboard, DOPO gli header.
        var bitboardsBaseOffset = headersTotalSize;
        var currentInternalOffset = 0;

        for (var i = 0; i < totalBitboards; i++)
        {
            var startCacheLine = currentInternalOffset / Constants.CacheLine;
            var endCacheLine = (currentInternalOffset + bitboardSize - 1) / Constants.CacheLine;
            if (startCacheLine != endCacheLine)
            {
                currentInternalOffset = currentInternalOffset.AlignUp64();
            }
        
            // FIX 1: L'offset finale è la base del blocco + l'offset interno.
            // Lo memorizziamo in BYTE per coerenza.
            bitboardOffsets[i] = bitboardsBaseOffset + currentInternalOffset;
        
            currentInternalOffset += bitboardSize;
        }
        // La dimensione totale è semplicemente l'offset finale dell'ultimo elemento + la sua dimensione
        var bitboardsTotalSize = currentInternalOffset;

        // 3. Assemblaggio Finale
        var slotSize = headersTotalSize + bitboardsTotalSize;
    
        return new MemoryLayout(slotSize, headerStride, bitboardSize, headersBaseOffset, bitboardOffsets);
    }
}