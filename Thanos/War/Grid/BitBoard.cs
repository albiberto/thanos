namespace Thanos.War.Grid;

public readonly ref struct Bitboard(Span<ulong> bitboard)
{
    private readonly Span<ulong> _bitboard = bitboard;
    
    public ReadOnlySpan<ulong> GetRawData() => _bitboard;

    public void Set(ushort position1D) => _bitboard[position1D >> 6] |= 1UL << (position1D & 63);

    public void Clear(ushort position1D) => _bitboard[position1D >> 6] &= ~(1UL << (position1D & 63));

    public bool IsSet(ushort position1D)
    {
        var index = position1D >> 6;
        var mask = 1UL << (position1D & 63);
        return (_bitboard[index] & mask) != 0;
    }
}