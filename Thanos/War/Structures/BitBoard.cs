using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace Thanos.War.Structures;

[method: MethodImpl(MethodImplOptions.AggressiveInlining)]
public readonly ref partial struct Bitboard(Span<byte> raw)
{
    private readonly ref ulong _first = ref Unsafe.As<byte, ulong>(ref MemoryMarshal.GetReference(raw));
    private readonly int _ulongsCount = raw.Length / 8;

    public readonly Span<byte> Raw = raw;
    public readonly Span<ulong> Buffer = MemoryMarshal.Cast<byte, ulong>(raw);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Set(ushort position1D)
    {
        ref var chunk = ref Unsafe.Add(ref _first, position1D >> 6);
        chunk |= 1UL << (position1D & 63);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Unset(ushort position1D)
    {
        ref var chunk = ref Unsafe.Add(ref _first, position1D >> 6);
        chunk &= ~(1UL << (position1D & 63));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsSet(ushort position1D)
    {
        ref var chunk = ref Unsafe.Add(ref _first, position1D >> 6);
        return (chunk & (1UL << (position1D & 63))) != 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsUnset(ushort position1D)
    {
        ref var chunk = ref Unsafe.Add(ref _first, position1D >> 6);
        return (chunk & (1UL << (position1D & 63))) == 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int PopCount()
    {
        switch (_ulongsCount)
        {
            case 1:
                return BitOperations.PopCount(_first);
            case 2:
                return BitOperations.PopCount(_first) + BitOperations.PopCount(Unsafe.Add(ref _first, 1));
            case 3:
                return BitOperations.PopCount(_first) + BitOperations.PopCount(Unsafe.Add(ref _first, 1)) + BitOperations.PopCount(Unsafe.Add(ref _first, 2));
            case 4:
                return BitOperations.PopCount(_first) + BitOperations.PopCount(Unsafe.Add(ref _first, 1)) + BitOperations.PopCount(Unsafe.Add(ref _first, 2)) + BitOperations.PopCount(Unsafe.Add(ref _first, 3));
            case 5:
                return BitOperations.PopCount(_first) + BitOperations.PopCount(Unsafe.Add(ref _first, 1)) + BitOperations.PopCount(Unsafe.Add(ref _first, 2)) + BitOperations.PopCount(Unsafe.Add(ref _first, 3)) + BitOperations.PopCount(Unsafe.Add(ref _first, 4));
            case 6:
                return BitOperations.PopCount(_first) + BitOperations.PopCount(Unsafe.Add(ref _first, 1)) + BitOperations.PopCount(Unsafe.Add(ref _first, 2)) + BitOperations.PopCount(Unsafe.Add(ref _first, 3)) + BitOperations.PopCount(Unsafe.Add(ref _first, 4)) + BitOperations.PopCount(Unsafe.Add(ref _first, 5));
        }

        var count = 0;
        for (var i = 0; i < _ulongsCount; i++) count += BitOperations.PopCount(Unsafe.Add(ref _first, i));
        return count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopyTo(Bitboard destination) => Unsafe.CopyBlockUnaligned(ref MemoryMarshal.GetReference(destination.Raw), ref MemoryMarshal.GetReference(Raw), (uint)Raw.Length);
}