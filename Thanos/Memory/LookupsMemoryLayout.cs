using Thanos.Common;
using Thanos.SourceGen;

namespace Thanos.Memory;

public readonly unsafe struct LookupsMemoryLayout
{
    public readonly MemoryBlock Coordinates;
    public readonly MemoryBlock Neighbors;

    public readonly nuint TotalSize;

    public LookupsMemoryLayout(ushort area)
    {
        var coordsByteSize = area * sizeof(Coordinate);
        
        var neighborsOffset = coordsByteSize.AlignUp64();
        var neighborsLength = area * 4;
        var neighborsByteSize = neighborsLength * sizeof(ushort);

        Coordinates = new MemoryBlock(0, area);
        Neighbors = new MemoryBlock(neighborsOffset, neighborsLength);
        
        TotalSize = (nuint)(neighborsOffset + neighborsByteSize).AlignUp64();
    }
}