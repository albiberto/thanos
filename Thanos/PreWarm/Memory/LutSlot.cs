using Thanos.SourceGen;

namespace Thanos.PreWarm.Memory;

public readonly ref struct LutSlot(ReadOnlySpan<double> positionalScores, ReadOnlySpan<Coordinate> conversionMap)
{
    public readonly ReadOnlySpan<double> PositionalScores = positionalScores;
    public readonly ReadOnlySpan<Coordinate> ConversionMap = conversionMap;
}