using Thanos.Common;
using Thanos.SourceGen;

namespace Thanos.Memory;

public readonly unsafe struct LookupsMemoryLayout
{
    public readonly byte Width;
    public readonly byte Height;
    public readonly ushort Area;

    public readonly MemoryBlock Coordinates;
    public readonly MemoryBlock Neighbors;

    public readonly nuint TotalSize;

    public LookupsMemoryLayout(byte width, byte height, int area)
    {
        Width = width;
        Height = height;
        Area = (ushort)area;

        var coordsByteSize = area * sizeof(Coordinate);
        Coordinates = new MemoryBlock(0, area);

        var neighborsOffset = coordsByteSize.AlignUp64();
        var neighborsLength = area * 4;
        
        Neighbors = new MemoryBlock(neighborsOffset, neighborsLength);

        var neighborsByteSize = neighborsLength * sizeof(ushort);

        TotalSize = (nuint)(neighborsOffset + neighborsByteSize).AlignUp64();
    }
}