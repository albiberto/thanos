using System.Runtime.CompilerServices;
using Thanos.Common;

namespace Thanos.PreWarm;

public readonly ref struct NeighborsGrid(ReadOnlySpan<ushort> neighbors)
{
    private readonly ReadOnlySpan<ushort> _neighbors = neighbors;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort Get(ushort position, byte move) => _neighbors[position * 4 + move.NumberOfTrailingZeros()];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsValid(ushort position) => position != ushort.MaxValue;
}