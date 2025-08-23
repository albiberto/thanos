using Thanos.Memory;

namespace Thanos.War.Grid.Memory;

public readonly unsafe struct WarGridMemoryLayout
{
    private const int BitBoards = 3;

    public readonly int GeographySize;
    public readonly int BitboardsSize;
    
    // RINOMINATO E CORRETTO:
    public readonly int BitboardStride;   // Dimensione in byte allineata per bitboard

    public readonly int NeighborsBoardSize;

    public readonly int Size;
    
    public WarGridMemoryLayout(int area)
    {
        GeographySize = sizeof(Geography).AlignUp();
        
        var bitboardStrideInUlongs = (area + 63) / 64; 
        BitboardStride = (bitboardStrideInUlongs * sizeof(ulong)).AlignUp();
        BitboardsSize = BitboardStride * BitBoards;
        
        NeighborsBoardSize = area * 4 * sizeof(ushort);
        
        Size = GeographySize + BitboardsSize + NeighborsBoardSize;
    }
}