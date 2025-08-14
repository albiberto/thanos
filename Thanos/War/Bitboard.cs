using System.Runtime.CompilerServices;

namespace Thanos.War;

/// <summary>
/// Un wrapper ref struct che rappresenta e gestisce un singolo bitboard.
/// Fornisce un'API sicura e leggibile sopra un'area di memoria grezza.
/// </summary>
public readonly unsafe ref struct Bitboard(ulong* ptr, uint segmentCount)
{
    private readonly Span<ulong> _data = new(ptr, (int)segmentCount);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Set(ushort position1D) => _data[position1D >> 6] |= 1UL << (position1D & 63);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear(ushort position1D) => _data[position1D >> 6] &= ~(1UL << (position1D & 63));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsSet(ushort position1D)
    {
        var index = position1D >> 6;
        var mask = 1UL << (position1D & 63);
        return (_data[index] & mask) != 0;
    }
    
    public void ClearAll() => _data.Clear();
}