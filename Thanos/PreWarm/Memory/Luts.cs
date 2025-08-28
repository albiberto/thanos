using Thanos.SourceGen;

namespace Thanos.PreWarm.Memory;

public readonly struct Luts(double[] positionalScores, Coordinate[] conversionsMap)
{
    public readonly double[] PositionalScores = positionalScores;
    public readonly Coordinate[] ConversionsMap = conversionsMap;
}