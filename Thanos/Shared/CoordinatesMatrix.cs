using System.Runtime.CompilerServices;
using Thanos.SourceGen;

namespace Thanos.Shared;

public readonly ref struct CoordinatesMatrix(ReadOnlySpan<Coordinate> buffer)
{
    private readonly ReadOnlySpan<Coordinate> _buffer = buffer;

    public Coordinate this[ushort position] 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _buffer[position];
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Coordinate Get(ushort position) => _buffer[position];
}