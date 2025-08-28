using System.Runtime.InteropServices;
using Thanos.SourceGen;

namespace Thanos.PreWarm.Memory;

public sealed unsafe class LutProvider : IDisposable
{
    private readonly LutMemoryLayout _layout;
    private readonly void* _memoryBlock;
    
    private bool _disposed;

    public LutProvider(int maxWidth, int maxArea)
    {
        _layout = new LutMemoryLayout(maxWidth, maxArea);
        _memoryBlock = NativeMemory.AlignedAlloc(_layout.TotalSizeInBytes, Constants.CacheLine);

        Burn(maxWidth);
    }

    /// <summary>
    /// Restituisce una struct "contenitore" con tutte le LUT per la larghezza specificata.
    /// Questa operazione è a costo quasi zero (creazione di una struct sullo stack).
    /// </summary>
    public LutSlot Get(int width)
    {
        var positionalInfo = _layout.PositionalScoreLayout[width];
        var positionalSpan = new ReadOnlySpan<double>((byte*)_memoryBlock + positionalInfo.Offset, positionalInfo.Area);

        var conversionInfo = _layout.ConversionMapLayout[width];
        var conversionSpan = new ReadOnlySpan<Coordinate>((byte*)_memoryBlock + conversionInfo.Offset, conversionInfo.Area);

        return new LutSlot(positionalSpan, conversionSpan);
    }
    
    private void Burn(int maxWidth)
    {
        Parallel.For(1, maxWidth + 1, width =>
        {
            PositionalScoreCache.Build(width, new Span<double>((byte*)_memoryBlock + _layout.PositionalScoreLayout[width].Offset, _layout.PositionalScoreLayout[width].Area));
            ConversionMapCache.Build(width, new Span<Coordinate>((byte*)_memoryBlock + _layout.ConversionMapLayout[width].Offset, _layout.ConversionMapLayout[width].Area));
        });
    }

    public void Dispose()
    {
        if (_disposed) return;
        NativeMemory.Free(_memoryBlock);
        _disposed = true;
        GC.SuppressFinalize(this);
    }
    
    ~LutProvider()
    {
        Dispose();
    }
}