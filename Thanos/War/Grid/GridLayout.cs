using Thanos.Memory;
using Thanos.War.Grid;

public readonly unsafe struct GridLayout
{
    private const int BitBoards = 3;

    public readonly int GeographySize;
    public readonly int BitboardsSize;
    
    // RINOMINATO E CORRETTO:
    public readonly int BitboardStrideInUlongs; // Numero di ulong per bitboard
    public readonly int BitboardStrideInBytes;   // Dimensione in byte allineata per bitboard

    public readonly int Size;
    
    public GridLayout(int area)
    {
        GeographySize = sizeof(Geography).AlignUp();
        
        // 1. Calcola il numero PURO di ulong necessari (arrotondando per eccesso)
        BitboardStrideInUlongs = (area + 63) / 64; 
        
        // 2. Calcola la dimensione in byte e ALLINEALA
        var unalignedByteSize = BitboardStrideInUlongs * sizeof(ulong);
        BitboardStrideInBytes = unalignedByteSize.AlignUp();
        
        // 3. La dimensione totale è 3 volte lo stride allineato
        BitboardsSize = BitboardStrideInBytes * BitBoards;
        
        Size = GeographySize + BitboardsSize;
    }
}