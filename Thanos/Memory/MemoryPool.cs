using System.Buffers;
using Thanos.Enums;

namespace Thanos.Memory;

public sealed class MemoryPool : IDisposable
{
    private readonly IMemoryOwner<byte> _memoryOwner;
    private readonly Memory<byte> _memory;
    private MemoryHandle _memoryHandle;
    private long _currentOffset;
    
    // Il context e la mappa non sono più readonly, vengono impostati da Reset
    private GameContext _context; 

    // COSTRUTTORE SEMPLIFICATO: alloca solo la memoria
    public MemoryPool(in GameContext context, int maxNodes = Constants.MaxNodes)
    {
        _context = context;
        
        _memoryOwner = MemoryPool<byte>.Shared.Rent(_context.Layout.SlotSize * maxNodes); // Circa 2GB
        _memory = _memoryOwner.Memory;
        _memoryHandle = _memory.Pin();
    }

    /// <summary>
    /// Tenta di ottenere il prossimo slot di memoria e restituisce la vista MemorySlot già pronta.
    /// </summary>
    public MemorySlot GetNext()
    {
        var slotSize = _context.Layout.SlotSize;
        
        var newOffset = Interlocked.Add(ref _currentOffset, slotSize);
        var startOffset = (int)(newOffset - slotSize);
        
        var slotSpan = _memory.Span.Slice(startOffset, slotSize);
        
        return new MemorySlot(slotSpan, in _context);
    }

    public void Reset(in GameContext context)
    {
        _context = context;
        _currentOffset = 0;
    }
    
    public void Dispose()
    {
        _memoryOwner.Dispose();
        _memoryHandle.Dispose();
    }
}