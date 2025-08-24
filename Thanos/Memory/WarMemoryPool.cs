using System.Buffers;

namespace Thanos.Memory;

public sealed class WarMemoryPool : IDisposable
{
    private GameContext _context; 
    
    private readonly IMemoryOwner<byte> _memoryOwner;
    private readonly Memory<byte> _memory;
    private MemoryHandle _memoryHandle;
    
    private long _offset;
    
    public WarMemoryPool(in GameContext context, int maxNodes)
    {
        _context = context;
        
        _memoryOwner = MemoryPool<byte>.Shared.Rent(_context.Layout.WarSlotSize * maxNodes);
        
        _memory = _memoryOwner.Memory;
        _memory.Span.Clear();
        _memoryHandle = _memory.Pin();
    }
    
    public MemorySlot GetNext()
    {
        var slotSize = _context.Layout.WarSlotSize;
        
        var newOffset = Interlocked.Add(ref _offset, slotSize);
        var startOffset = (int)(newOffset - slotSize);
        
        var slotSpan = _memory.Span.Slice(startOffset, slotSize);
        
        return new MemorySlot(slotSpan, in _context);
    }

    public void Clear()
    {
        _memory.Span.Clear();
        _offset = 0;
    }
    
    public void Reset(in GameContext context) => _context = context;

    public void Dispose()
    {
        _memoryOwner.Dispose();
        _memoryHandle.Dispose();
    }
}