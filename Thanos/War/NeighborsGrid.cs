using System.Runtime.CompilerServices;
using Thanos.Common;

namespace Thanos.War;

public readonly ref struct NeighborsGrid(int area, ReadOnlySpan<ushort> neighbors)
{
    private readonly ReadOnlySpan<ushort> _neighbors = neighbors;

    public int Area { get; } = area;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort Get(ushort position, byte move) => _neighbors[position * 4 + move.NumberOfTrailingZeros()];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsValid(ushort position) => position != ushort.MaxValue;
}