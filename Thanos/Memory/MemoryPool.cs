using System.Buffers;
using Thanos.MCST;
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

        var startOffset = (int)(newOffset - slotSize);
        // Il pool ora distribuisce Span<byte> sicuri
        var slotSpan = _poolMemory.Span.Slice(startOffset, slotSize);
        slot = new MemorySlot(slotSpan, _context, _layout);
        return true;
    }
    
    /// <summary>
    /// NUOVO METODO: Dato un puntatore a un nodo, restituisce la "vista" MemorySlot
    /// per interagire con l'intero blocco di memoria di quel nodo.
    /// </summary>
    public unsafe MemorySlot GetSlotFromPointer(Node* nodePtr)
    {
        var slotSpan = new Span<byte>(nodePtr, _layout.Sizes.Slot);
        return new MemorySlot(slotSpan, _context, _layout);
    }

    public void Reset() => _currentOffset = 0;
    
    public void Dispose() => _memoryOwner.Dispose();
}