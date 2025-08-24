using Thanos.Common;
using Thanos.Memory;

namespace Thanos.War.Grid.Memory;

public readonly unsafe struct WarGridMemoryLayout
{
    public readonly int GeographySize;

    private const int BitBoards = 3;
    public readonly int BitboardStride;
    public readonly int BitboardsSize;

    public readonly int NeighborsBoardSize;

    public readonly int Size;
    
    public WarGridMemoryLayout(int area, int neighborsLenght)
    {
        GeographySize = sizeof(Geography).AlignUp();
        
        var bitboardStrideInUlongs = (area + 63) / 64; 
        BitboardStride = (bitboardStrideInUlongs * sizeof(ulong)).AlignUp();
        BitboardsSize = BitboardStride * BitBoards;
        
        NeighborsBoardSize = (neighborsLenght * sizeof(ushort)).AlignUp();
        
        Size = GeographySize + BitboardsSize + NeighborsBoardSize;
    }
}