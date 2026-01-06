using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Thanos.War.Structures;

[method: MethodImpl(MethodImplOptions.AggressiveInlining)]
public readonly ref partial struct Bitboard(Span<byte> raw)
{
    private readonly ref ulong _root = ref Unsafe.As<byte, ulong>(ref MemoryMarshal.GetReference(raw));
    private readonly int _ulongsCount = raw.Length / 8;

    public readonly Span<byte> Raw = raw;
    public readonly Span<ulong> Buffer = MemoryMarshal.Cast<byte, ulong>(raw);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Set(ushort position1D)
    {
        ref var chunk = ref Unsafe.Add(ref _root, position1D >> 6);
        chunk |= 1UL << (position1D & 63);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Unset(ushort position1D)
    {
        ref var chunk = ref Unsafe.Add(ref _root, position1D >> 6);
        chunk &= ~(1UL << (position1D & 63));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsSet(ushort position1D)
    {
        ref var chunk = ref Unsafe.Add(ref _root, position1D >> 6);
        return (chunk & (1UL << (position1D & 63))) != 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsUnset(ushort position1D)
    {
        ref var chunk = ref Unsafe.Add(ref _root, position1D >> 6);
        return (chunk & (1UL << (position1D & 63))) == 0;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear() => Buffer.Clear();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopyTo(Bitboard destination) => Unsafe.CopyBlockUnaligned(ref MemoryMarshal.GetReference(destination.Raw), ref MemoryMarshal.GetReference(Raw), (uint)Raw.Length);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopyFrom(in Bitboard source) => Unsafe.CopyBlockUnaligned(ref MemoryMarshal.GetReference(Raw), ref MemoryMarshal.GetReference(source.Raw), (uint)Raw.Length);
}