using System.Runtime.InteropServices;

namespace Thanos.War;

public readonly ref struct Bitboard(Span<byte> memory)
{
    private readonly Span<ulong> _bitboard = MemoryMarshal.Cast<byte, ulong>(memory);

    public Span<ulong> Raw => _bitboard;

    public void Clear() => _bitboard.Clear();

    public void Set(ushort position1D) => _bitboard[position1D >> 6] |= 1UL << (position1D & 63);

    public void Unset(ushort position1D) => _bitboard[position1D >> 6] &= ~(1UL << (position1D & 63));

    public bool IsSet(ushort position1D)
    {
        var index = position1D >> 6;
        var mask = 1UL << (position1D & 63);
        return (_bitboard[index] & mask) != 0;
    }

    public bool IsUnset(ushort position1D)
    {
        var index = position1D >> 6;
        var mask = 1UL << (position1D & 63);
        return (_bitboard[index] & mask) == 0;
    }
    
    public void Xor(Bitboard other)
    {
        var otherData = other.Raw;
        for (var i = 0; i < Raw.Length; i++) Raw[i] ^= otherData[i];
    }
    
    public void Or(in Bitboard other)
    {
        var otherData = other.Raw;
        for (var i = 0; i < Raw.Length; i++) Raw[i] |= otherData[i];
    }
}