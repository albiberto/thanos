using System.Numerics;
using System.Runtime.InteropServices;

namespace Thanos.War.Structures;

public readonly ref struct Bitboard(Span<byte> raw)
{
    public readonly ReadOnlySpan<byte> Raw = raw;
    public readonly Span<ulong> Buffer = MemoryMarshal.Cast<byte, ulong>(raw);

    public void Clear() => Buffer.Clear();

    public void Set(ushort position1D) => Buffer[position1D >> 6] |= 1UL << (position1D & 63);

    public void Unset(ushort position1D) => Buffer[position1D >> 6] &= ~(1UL << (position1D & 63));

    public bool IsSet(ushort position1D)
    {
        var index = position1D >> 6;
        var mask = 1UL << (position1D & 63);
        var result = (Buffer[index] & mask) != 0;

        return result;
    }

    public bool IsUnset(ushort position1D)
    {
        var index = position1D >> 6;
        var mask = 1UL << (position1D & 63);
        var result = (Buffer[index] & mask) == 0;

        return result;
    }

    public void Xor(Bitboard other)
    {
        var otherData = other.Buffer;
        for (var i = 0; i < Buffer.Length; i++) Buffer[i] ^= otherData[i];
    }

    public void Or(in Bitboard other)
    {
        var otherData = other.Buffer;
        for (var i = 0; i < Buffer.Length; i++) Buffer[i] |= otherData[i];
    }

    public int PopCount()
    {
        var count = 0;
        foreach (var chunk in Buffer) count += BitOperations.PopCount(chunk);

        return count;
    }

    public void CopyTo(Bitboard destination) => Buffer.CopyTo(destination.Buffer);
}