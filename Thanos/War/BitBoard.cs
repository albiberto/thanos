using System.Numerics;
using System.Runtime.InteropServices;

namespace Thanos.War;

public readonly ref struct Bitboard(Span<byte> memory)
{
    public readonly Span<ulong> Memory = MemoryMarshal.Cast<byte, ulong>(memory);
    public readonly ReadOnlySpan<byte> Raw = memory;

    public void Clear() => Memory.Clear();

    public void Set(ushort position1D) => Memory[position1D >> 6] |= 1UL << (position1D & 63);

    public void Unset(ushort position1D) => Memory[position1D >> 6] &= ~(1UL << (position1D & 63));
    

    public bool IsSet(ushort position1D)
    {
        var index = position1D >> 6;
        var mask = 1UL << (position1D & 63);
        var result = (Memory[index] & mask) != 0;

        return result;
    }

    public bool IsUnset(ushort position1D)
    {
        var index = position1D >> 6;
        var mask = 1UL << (position1D & 63);
        var result = (Memory[index] & mask) == 0;

        return result;
    }

    public void Xor(Bitboard other)
    {
        var otherData = other.Memory;
        for (var i = 0; i < Memory.Length; i++) Memory[i] ^= otherData[i];
    }

    public void Or(in Bitboard other)
    {
        var otherData = other.Memory;
        for (var i = 0; i < Memory.Length; i++) Memory[i] |= otherData[i];
    }
    
    public int PopCount()
    {
        var count = 0;
        foreach (var chunk in this.Memory) count += BitOperations.PopCount(chunk);

        return count;
    }
    
    public void CopyTo(Bitboard destination) => Memory.CopyTo(destination.Memory);
}