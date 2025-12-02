using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Thanos.Shared;
using Thanos.SourceGen;

namespace Thanos.Memory;

public sealed unsafe class LookupsMemoryPool : IDisposable
{
    private readonly LookupsMemoryLayout _layout;
    private readonly byte* _basePointer;
    
    private ReadOnlySpan<Coordinate> _coordinatesMemory => new(_basePointer + _layout.Coordinates.Offset, _layout.Coordinates.Length);
    private ReadOnlySpan<ushort> _neighborsMemory => new(_basePointer + _layout.Neighbors.Offset, _layout.Neighbors.Length);

    public CoordinatesMatrix CoordinatesMatrix => new(_coordinatesMemory);
    public NeighborsMatrix NeighborsMatrix => new(_neighborsMemory);

    private LookupsMemoryPool(byte width, byte height, int area)
    {
        _layout = new(width, height, area);
        _basePointer = (byte*)NativeMemory.AlignedAlloc(_layout.TotalSize, Constants.CacheLine);

        NativeMemory.Clear(_basePointer, _layout.TotalSize);

        var coordsSpan = new Span<Coordinate>(_basePointer + _layout.Coordinates.Offset, _layout.Coordinates.Length);
        CoordinatesBuilder.Populate(width, height, coordsSpan);

        var neighborsSpan = new Span<ushort>(_basePointer + _layout.Neighbors.Offset, _layout.Neighbors.Length);
        NeighborsBuilder.Populate(width, height, neighborsSpan);
    }

    public static LookupsMemoryPool Small => new(Constants.Small.Width, Constants.Small.Height, Constants.Small.Area);
    public static LookupsMemoryPool Medium => new(Constants.Medium.Width, Constants.Medium.Height, Constants.Medium.Area);
    public static LookupsMemoryPool Large => new(Constants.Large.Width, Constants.Large.Height, Constants.Large.Area);

    public void Dispose() => NativeMemory.AlignedFree(_basePointer);
}