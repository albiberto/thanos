using System.Numerics;
using System.Runtime.CompilerServices;

namespace Thanos.Shared;

public readonly ref struct NeighborsMatrix(ReadOnlySpan<ushort> buffer)
{
    private readonly ReadOnlySpan<ushort> _buffer = buffer;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort Get(ushort currentPos, byte moveMask) => _buffer[currentPos * 4 + BitOperations.TrailingZeroCount(moveMask)];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort GetAt(ushort currentPos, int moveIndex) => _buffer[currentPos * 4 + moveIndex];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsValid(ushort position) => position != ushort.MaxValue;
}