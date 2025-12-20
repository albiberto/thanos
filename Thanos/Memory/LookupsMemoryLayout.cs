using Thanos.SourceGen;

namespace Thanos.Memory;

public readonly struct LookupsMemoryLayout
{
    public readonly MemoryBlock Coordinates;
    public readonly MemoryBlock Neighbors;
    public readonly nuint TotalSize;

    public LookupsMemoryLayout(ushort area)
    {
        Coordinates = MemoryBlock.CreateUp64<Coordinate>(0, area);
        Neighbors = MemoryBlock.CreateUp64<ushort>(Coordinates.Next, area * 4);
        
        TotalSize = Neighbors.Next;
    }
}