using System.Runtime.InteropServices;
using Thanos.PreWarm;
using Thanos.SourceGen;

namespace Thanos.Memory;

public sealed unsafe class LookupsMemoryPool : IDisposable
{
    private readonly byte* _basePointer;
    private readonly LookupsMemoryLayout _layout;

    public NeighborsGrid NeighborsGrid => new(NeighborsBuffer);
    public ReadOnlySpan<Coordinate> ConversionsMap => ConversionsMapBuffer;
    public ReadOnlySpan<float> PositionalScores => PositionalScoresBuffer;

    public LookupsMemoryPool(in LookupsMemoryLayout layout)
    {
        _layout = layout;

        _basePointer = (byte*)NativeMemory.AlignedAlloc(_layout.TotalSize, Constants.CacheLine);
        NativeMemory.Clear(_basePointer, _layout.TotalSize);

        NeighborsGridBuilder.Build(layout.Width, NeighborsBuffer);
        ConversionMapBuilder.Build(layout.Width, ConversionsMapBuffer);
        PositionalScoreBuilder.Build(layout.Width, PositionalScoresBuffer);

        Console.WriteLine($"[LookupsMemoryPool] Allocated {(double)_layout.TotalSize / (1024 * 1024):F3} MB for unmanaged LUTs.");
    }

    private Span<ushort> NeighborsBuffer => new(_basePointer + _layout.NeighborsOffset, _layout.NeighborsLength);
    private Span<Coordinate> ConversionsMapBuffer => new(_basePointer + _layout.ConversionMapOffset, _layout.ConversionMapLength);
    private Span<float> PositionalScoresBuffer => new(_basePointer + _layout.PositionalScoreOffset, _layout.PositionalScoreLength); // <-- Aggiunto

    public void Dispose() => NativeMemory.Free(_basePointer);
}