using Thanos.Common;
using Thanos.War;

namespace Thanos.Memory;

public unsafe class MemoryLayoutBuilder(int area, int snakeCount)
{
    public MemoryLayout Build()
    {
        // 1. Calcolo Blocco Headers (invariato)
        var headerStride = sizeof(WarSnakeHeader);
        var headersTotalSize = (headerStride * snakeCount).AlignUp64();
        var headersBaseOffset = 0;

        // 2. Calcolo Blocco Bitboards (con il nuovo ordine)
        var ulongsNeeded = (area + 63) / 64;
        var bitboardSize = ulongsNeeded * sizeof(ulong);

        // Il numero totale di bitboard non cambia
        var totalBitboards = LayoutConstants.GlobalBitboardCount + snakeCount + 1; // +1 per AllSnakes
        var bitboardOffsets = new int[totalBitboards];

        var bitboardsBaseOffset = headersTotalSize;
        var currentInternalOffset = 0;
        var offsetIndex = 0; // Usiamo un indice separato per riempire l'array

        // --- NUOVO ORDINE DI CALCOLO ---

        // Funzione helper per evitare ripetizioni
        int AddBitboardOffset()
        {
            // La logica di allineamento alla cache line è corretta e rimane
            var startCacheLine = currentInternalOffset / Constants.CacheLine;
            var endCacheLine = (currentInternalOffset + bitboardSize - 1) / Constants.CacheLine;
            if (startCacheLine != endCacheLine) currentInternalOffset = currentInternalOffset.AlignUp64();
            var absoluteOffset = bitboardsBaseOffset + currentInternalOffset;
            currentInternalOffset += bitboardSize;
            return absoluteOffset;
        }

        // A. Prima i bitboard globali
        bitboardOffsets[offsetIndex++] = AddBitboardOffset(); // FoodBitboard
        bitboardOffsets[offsetIndex++] = AddBitboardOffset(); // HazardsBitboard
        bitboardOffsets[offsetIndex++] = AddBitboardOffset(); // AllSnakesBitboard

        // B. Poi tutti i bitboard dei singoli serpenti
        for (var i = 0; i < snakeCount; i++) bitboardOffsets[offsetIndex++] = AddBitboardOffset();

        var bitboardsTotalSize = currentInternalOffset;

        // 3. Assemblaggio Finale (invariato)
        var slotSize = headersTotalSize + bitboardsTotalSize;
        return new MemoryLayout(slotSize, headerStride, bitboardSize, headersBaseOffset, bitboardOffsets);
    }
}