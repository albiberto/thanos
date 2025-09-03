using Thanos.SourceGen;

namespace Thanos.PreWarm.Memory;

public readonly struct Luts(float[] positionalScores, Coordinate[] conversionsMap)
{
    public readonly float[] PositionalScores = positionalScores;
    public readonly Coordinate[] ConversionsMap = conversionsMap;
}