using Thanos.Common;
using Thanos.War;

namespace Thanos.Memory;

public unsafe class MemoryLayoutBuilder(int area, int snakeCount)
{
    public static MemoryLayout Worst => new MemoryLayoutBuilder(Constants.Large, Constants.MaxSnakesCount).Build();

    public MemoryLayout Build()
    {
        // NUOVO: Definiamo lo spazio necessario per il nostro dato globale all'inizio.
        const int playerToMoveIndexSize = sizeof(int);

        // 1. Calcolo Blocco Headers
        var headerStride = sizeof(WarSnakeHeader);
        var headersTotalSize = (headerStride * snakeCount).AlignUp64();
        // MODIFICATO: L'offset di base degli header non è più 0.
        // Inizia subito dopo lo spazio che abbiamo riservato.
        var headersBaseOffset = playerToMoveIndexSize;

        // 2. Calcolo Blocco Bitboards
        var ulongsNeeded = (area + 63) / 64;
        var bitboardSize = ulongsNeeded * sizeof(ulong);

        var totalBitboards = LayoutConstants.GlobalBitboardCount + snakeCount + 1;
        var bitboardOffsets = new int[totalBitboards];

        // MODIFICATO: L'offset di base dei bitboard ora tiene conto
        // sia dello spazio per l'indice che della dimensione totale degli header.
        var bitboardsBaseOffset = headersBaseOffset + headersTotalSize;
        var currentInternalOffset = 0;
        var offsetIndex = 0;

        int AddBitboardOffset()
        {
            var startCacheLine = currentInternalOffset / Constants.CacheLine;
            var endCacheLine = (currentInternalOffset + bitboardSize - 1) / Constants.CacheLine;
            if (startCacheLine != endCacheLine) currentInternalOffset = currentInternalOffset.AlignUp64();
            var absoluteOffset = bitboardsBaseOffset + currentInternalOffset;
            currentInternalOffset += bitboardSize;
            return absoluteOffset;
        }

        bitboardOffsets[offsetIndex++] = AddBitboardOffset(); // Food
        bitboardOffsets[offsetIndex++] = AddBitboardOffset(); // Hazards
        bitboardOffsets[offsetIndex++] = AddBitboardOffset(); // AllSnakes

        for (var i = 0; i < snakeCount; i++) bitboardOffsets[offsetIndex++] = AddBitboardOffset();

        var bitboardsTotalSize = currentInternalOffset;

        // 3. Assemblaggio Finale
        // MODIFICATO: La dimensione totale dello slot ora include lo spazio per l'indice.
        var slotSize = playerToMoveIndexSize + headersTotalSize + bitboardsTotalSize;
        return new MemoryLayout(slotSize, headerStride, bitboardSize, headersBaseOffset, bitboardOffsets);
    }
}