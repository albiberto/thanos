using System.Runtime.CompilerServices;

namespace Thanos.War;

public readonly ref struct Bitboard(Span<ulong> bitboard)
{
    private readonly Span<ulong> _bitboard = bitboard;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ReadOnlySpan<ulong> GetRawData() => _bitboard;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Set(ushort position1D) => _bitboard[position1D >> 6] |= 1UL << (position1D & 63);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear(ushort position1D) => _bitboard[position1D >> 6] &= ~(1UL << (position1D & 63));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsSet(ushort position1D)
    {
        var index = position1D >> 6;
        var mask = 1UL << (position1D & 63);
        return (_bitboard[index] & mask) != 0;
    }
    
    public void ClearAll() => _bitboard.Clear();
}