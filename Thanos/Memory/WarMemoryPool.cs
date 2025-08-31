using System;
using System.Runtime.InteropServices; // Necessario per NativeMemory
using System.Threading;
using Thanos.PreWarm.Memory;
using Thanos.SourceGen;

namespace Thanos.Memory;

// La classe deve essere marcata come 'unsafe' per permettere l'uso di puntatori
public sealed unsafe class WarMemoryPool : IDisposable
{
    private GameContext _context;
    private Luts _luts;

    // Sostituiamo Memory<byte> con un puntatore alla memoria non gestita
    private readonly byte* _basePointer;
    private readonly long _totalSize;
    
    private long _offset;

    public WarMemoryPool(in GameContext context, long maxNodes) // <-- Usiamo long per maxNodes
    {
        _context = context;
        _totalSize = context.Layout.WarSlotSize * maxNodes;

        _basePointer = (byte*)NativeMemory.AlignedAlloc((nuint)_totalSize, 64);
        NativeMemory.Clear(_basePointer, (nuint)_totalSize);
        
        _offset = 0;
    }
    
    public MemorySlot GetNext()
    {
        var slotSize = _context.Layout.WarSlotSize;
        var newOffset = Interlocked.Add(ref _offset, slotSize);

        if (newOffset > _totalSize) throw new InvalidOperationException("WarMemoryPool overflow: superata la dimensione massima del pool.");

        var startOffset = newOffset - slotSize;
        var slotPointer = _basePointer + startOffset;
        var slotSpan = new Span<byte>(slotPointer, slotSize);
        
        return new MemorySlot(slotSpan, ref _context, ref _luts);
    }

    public void Set(in GameContext context, in Luts luts)
    {
        _context = context;
        _luts = luts;
    }
    
    public void Reset() => _offset = 0;

    public void Dispose() => NativeMemory.Free(_basePointer);
}