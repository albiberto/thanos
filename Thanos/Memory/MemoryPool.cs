using System.Buffers; // Necessario per IMemoryOwner
using Thanos.MCST;
using Thanos.Memory;
using Thanos.War;

public sealed class MemoryPool : IDisposable
{
    private readonly IMemoryOwner<byte> _memoryOwner;
    private readonly Memory<byte> _poolMemory;
    private long _currentOffset;
    
    private readonly WarContext _context;
    private readonly MemoryLayout _layout;

    public MemoryPool(in WarContext context, in MemoryLayout layout)
    {
        _context = context;
        _layout = layout;
        _memoryOwner = MemoryPool<byte>.Shared.Rent((int)layout.PoolSize);
        _poolMemory = _memoryOwner.Memory;
        _poolMemory.Span.Clear(); 
    }

    /// <summary>
    /// Tenta di ottenere il prossimo Span di memoria libera per un nuovo slot.
    /// </summary>
    public bool TryGetNext(out Span<byte> slotSpan)
    {
        var slotSize = _layout.SlotSize;
        var newOffset = Interlocked.Add(ref _currentOffset, slotSize);

        if (newOffset > _poolMemory.Length)
        {
            slotSpan = default;
            return false;
        }

        var startOffset = (int)(newOffset - slotSize);
        slotSpan = _poolMemory.Span.Slice(startOffset, slotSize);
        return true;
    }

    /// <summary>
    /// Dato un puntatore a un nodo, restituisce la "vista" MemorySlot corrispondente.
    /// </summary>
    public unsafe MemorySlot GetSlotFromPointer(Node* nodePtr)
    {
        var slotSpan = new Span<byte>(nodePtr, _layout.SlotSize);
        return new MemorySlot(slotSpan, _context, _layout);
    }

    public void Reset() => _currentOffset = 0;
    
    // Restituisce la memoria al pool condiviso quando il nostro pool viene eliminato.
    public void Dispose() => _memoryOwner.Dispose();
}