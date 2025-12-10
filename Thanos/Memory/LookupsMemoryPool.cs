using System.Runtime.InteropServices;
using Thanos.Abstract;
using Thanos.Shared;
using Thanos.SourceGen;

namespace Thanos.Memory;

public sealed unsafe class LookupsMemoryPool : ILookupsMemoryPool
{
    private readonly byte* _basePointer;
    
    private readonly LookupsMemoryLayout _layout;

    
    public CoordinatesMatrix CoordinatesMatrix => new(CoordinatesSpan);
    public NeighborsMatrix NeighborsMatrix => new(NeighborsSpan);
    
    private ReadOnlySpan<Coordinate> CoordinatesSpan => new(_basePointer + _layout.Coordinates.Offset, _layout.Coordinates.Count<Coordinate>());
    private ReadOnlySpan<ushort> NeighborsSpan => new(_basePointer + _layout.Neighbors.Offset, _layout.Neighbors.Count<ushort>());

    public LookupsMemoryPool(byte width, byte height, ushort area)
    {
        _layout = new LookupsMemoryLayout(area);
        
        _basePointer = (byte*)NativeMemory.AlignedAlloc(_layout.TotalSize, Constants.CacheLine);
        NativeMemory.Clear(_basePointer, _layout.TotalSize);

        var coordsSpan = new Span<Coordinate>(_basePointer + _layout.Coordinates.Offset, _layout.Coordinates.Count<Coordinate>());
        var neighborsSpan = new Span<ushort>(_basePointer + _layout.Neighbors.Offset, _layout.Neighbors.Count<ushort>());
            
        CoordinatesBuilder.Populate(width, height, coordsSpan);
        NeighborsBuilder.Populate(width, height, neighborsSpan);
    }

    public void Dispose() => NativeMemory.AlignedFree(_basePointer);
}