namespace Thanos.PreWarm.Memory;

/// <summary>
/// Classe helper che calcola le dimensioni allineate per un set di LUT.
/// </summary>
public class LutMemoryLayout
{
    public int NeighborsSize { get; init; }
    public int PositionalScoresSize { get; init; }
    public int MapSize { get; init; }
    public int TotalSize => NeighborsSize + PositionalScoresSize + MapSize;
}