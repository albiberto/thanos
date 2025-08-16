using System.Buffers;
using Thanos.War;

namespace Thanos.Memory;

public sealed class MemoryPool : IDisposable
{
    private readonly IMemoryOwner<byte> _memoryOwner;
    private readonly Memory<byte> _poolMemory;
    private long _currentOffset;
    
    private readonly MemoryLayout _layout;
    private readonly WarContext _context;

    public MemoryPool(in WarContext context, in MemoryLayout layout)
    {
        _context = context;
        _layout = layout;
        // Il pool ora gestisce memoria gestita
        _memoryOwner = MemoryPool<byte>.Shared.Rent((int)layout.Sizes.Pool);
        _poolMemory = _memoryOwner.Memory;
        _poolMemory.Span.Clear(); // Azzera la memoria all'inizio
    }

    public bool TryGetNext(out MemorySlot slot)
    {
        var slotSize = _layout.Sizes.Slot;
        var newOffset = Interlocked.Add(ref _currentOffset, slotSize);

        if (newOffset > _poolMemory.Length)
        {
            slot = default;
            return false;
        }

        var startOffset = (int)(newOffset - slotSize);
        // Il pool ora distribuisce Span<byte> sicuri
        var slotSpan = _poolMemory.Span.Slice(startOffset, slotSize);
        slot = new MemorySlot(slotSpan, _context, _layout);
        return true;
    }

    public void Reset() => _currentOffset = 0;
    
    public void Dispose() => _memoryOwner.Dispose();
}