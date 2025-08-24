using System.Buffers;

namespace Thanos.Memory;

public sealed class WarMemoryPool : IDisposable
{
    private readonly IMemoryOwner<byte> _memoryOwner;
    private readonly Memory<byte> _memory;
    private MemoryHandle _memoryHandle;
    private long _currentOffset;
    
    private GameContext _context; 

    public WarMemoryPool(in GameContext context, int maxNodes = Constants.MaxNodes)
    {
        _context = context;
        
        _memoryOwner = MemoryPool<byte>.Shared.Rent(_context.Layout.WarSlotSize * maxNodes);
        _memory = _memoryOwner.Memory;
        _memoryHandle = _memory.Pin();
        _memory.Span.Clear();
    }
    
    public MemorySlot GetNext()
    {
        var slotSize = _context.Layout.WarSlotSize;
        
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