using Thanos.Common;
using Thanos.Memory.Pools;
using Thanos.War.Snake;

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
        // 1. Calcolo Blocco Headers
        var sizeOfHealth = sizeof(Health);
        var sizeOfAnatomy = sizeof(Anatomy);
        
        var headerStride = sizeOfHealth + sizeOfAnatomy;
        var headersTotalSize = (headerStride * _snakeCount).AlignUp64();
        
        var headersBaseOffset = 0;

        // 2. Calcolo Blocco Bitboards
        var ulongsNeeded = (_area + 63) / 64;
        var bitboardSize = ulongsNeeded * sizeof(ulong);

        var totalBitboards = LayoutConstants.GlobalBitboardCount + _snakeCount;
        var bitboardOffsets = new int[totalBitboards];
        
        var currentInternalOffset = 0;
        for (var i = 0; i < totalBitboards; i++)
        {
            var startCacheLine = currentInternalOffset / Constants.CacheLine;
            var endCacheLine = (currentInternalOffset + bitboardSize - 1) / Constants.CacheLine;
            if (startCacheLine != endCacheLine) currentInternalOffset = currentInternalOffset.AlignUp64();
            
            bitboardOffsets[i] = currentInternalOffset;
            currentInternalOffset += bitboardSize;
        }
        var bitboardsTotalSize = currentInternalOffset;

        // 3. Assemblaggio Finale
        var slotSize = headersTotalSize + bitboardsTotalSize;
        
        // Passiamo l'intero array di offset calcolati al costruttore del Layout
        return new MemoryLayout(slotSize, headerStride, bitboardSize, headersBaseOffset, bitboardOffsets);
    }
}