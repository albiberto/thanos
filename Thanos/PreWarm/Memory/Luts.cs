using Thanos.SourceGen;

namespace Thanos.PreWarm.Memory;

public readonly ref struct Luts(ReadOnlySpan<double>  positionalScores, ReadOnlySpan<Coordinate> conversionsMap)
{
    public readonly ReadOnlySpan<double> PositionalScores = positionalScores;
    public readonly ReadOnlySpan<Coordinate> ConversionsMap = conversionsMap;
}