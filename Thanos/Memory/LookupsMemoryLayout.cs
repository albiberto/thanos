using Thanos.Common; // Per AlignUp64
using Thanos.SourceGen;

namespace Thanos.Memory;

public readonly unsafe struct LookupsMemoryLayout
{
    public readonly byte Width;

    // --- 1. LUNGHEZZE (Element Count) ---
    public readonly int NeighborsLength;
    public readonly int ConversionMapLength;
    public readonly int PositionalScoreLength;

    // --- 2. OFFSET (Corretti) ---
    public readonly int NeighborsOffset;
    public readonly int ConversionMapOffset;
    public readonly int PositionalScoreOffset;

    // --- 3. DIMENSIONE TOTALE (Corretta) ---
    public readonly nuint TotalSize;

    private LookupsMemoryLayout(ushort area, byte width)
    {
        Width = width;

        // 1. Calcola le LUNGHEZZE (numero di elementi)
        NeighborsLength = area * 4;
        ConversionMapLength = area;
        PositionalScoreLength = area;

        // 2. Calcola le DIMENSIONI IN BYTE e allineale
        var neighborsByteSize = (NeighborsLength * sizeof(ushort)).AlignUp64();
        var conversionMapByteSize = (ConversionMapLength * sizeof(Coordinate)).AlignUp64();
        var positionalScoreByteSize = (PositionalScoreLength * sizeof(float)).AlignUp64();

        // 3. Calcola gli OFFSET basati sulle dimensioni allineate
        NeighborsOffset = 0;
        ConversionMapOffset = NeighborsOffset + neighborsByteSize;
        PositionalScoreOffset = ConversionMapOffset + conversionMapByteSize;

        // 4. Calcola la DIMENSIONE TOTALE (Corretta)
        TotalSize = (nuint)(PositionalScoreOffset + positionalScoreByteSize);
    }

    public static LookupsMemoryLayout Small => new(7 * 7, 7);
    public static LookupsMemoryLayout Medium => new(11 * 11, 11);
    public static LookupsMemoryLayout Large => new(19 * 19, 19);
}