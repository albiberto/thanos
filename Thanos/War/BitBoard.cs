using System.Numerics;
using System.Runtime.InteropServices;

namespace Thanos.War;

public readonly ref struct Bitboard(Span<byte> memory)
{
    public readonly Span<ulong> Memory = MemoryMarshal.Cast<byte, ulong>(memory);
    public readonly ReadOnlySpan<byte> Raw = memory;

    public void Clear()
    {
        // LOGGING
        Console.WriteLine("[Bitboard] Clearing all bits.");
        Memory.Clear();
    }

    public void Set(ushort position1D)
    {
        // LOGGING
        Console.WriteLine($"[Bitboard] Setting bit at position {position1D}.");
        Memory[position1D >> 6] |= 1UL << (position1D & 63);
    }

    public void Unset(ushort position1D)
    {
        // LOGGING
        Console.WriteLine($"[Bitboard] Unsetting bit at position {position1D}.");
        Memory[position1D >> 6] &= ~(1UL << (position1D & 63));
    }

    public bool IsSet(ushort position1D)
    {
        var index = position1D >> 6;
        var mask = 1UL << (position1D & 63);
        var result = (Memory[index] & mask) != 0;
        // LOGGING
        Console.WriteLine($"[Bitboard] Checking if bit {position1D} is set. Result: {result}");
        return result;
    }

    public bool IsUnset(ushort position1D)
    {
        var index = position1D >> 6;
        var mask = 1UL << (position1D & 63);
        var result = (Memory[index] & mask) == 0;
        // LOGGING
        Console.WriteLine($"[Bitboard] Checking if bit {position1D} is unset. Result: {result}");
        return result;
    }

    public void Xor(Bitboard other)
    {
        // LOGGING
        Console.WriteLine("[Bitboard] Performing XOR operation.");
        var otherData = other.Memory;
        for (var i = 0; i < Memory.Length; i++) Memory[i] ^= otherData[i];
    }

    public void Or(in Bitboard other)
    {
        // LOGGING
        Console.WriteLine("[Bitboard] Performing OR operation.");
        var otherData = other.Memory;
        for (var i = 0; i < Memory.Length; i++) Memory[i] |= otherData[i];
    }
    
    public int PopCount()
    {
        var count = 0;
        foreach (var chunk in this.Memory) count += BitOperations.PopCount(chunk);
        // LOGGING
        Console.WriteLine($"[Bitboard] PopCount result: {count}");
        return count;
    }
    
    public void CopyTo(Bitboard destination)
    {
        // LOGGING
        Console.WriteLine("[Bitboard] Performing deep copy to another Bitboard.");
        this.Memory.CopyTo(destination.Memory);
    }
}