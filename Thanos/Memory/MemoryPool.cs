using System.Buffers;
using Thanos.Enums;
using Thanos.MCST;
using Thanos.War;
// Necessario per IMemoryOwner

namespace Thanos.Memory;

public sealed class MemoryPool : IDisposable
{
    private readonly IMemoryOwner<byte> _memoryOwner;
    private readonly Memory<byte> _poolMemory;
    private long _currentOffset;
    
    private MemoryLayout _layout;

    public MemoryPool(in MemoryLayout layout, int poolSize)
    {
        _layout = layout;
        _memoryOwner = MemoryPool<byte>.Shared.Rent(poolSize);
        _poolMemory = _memoryOwner.Memory;
    }

    /// <summary>
    /// Tenta di ottenere il prossimo slot di memoria e restituisce la vista MemorySlot già pronta.
    /// </summary>
    public bool TryGetNext(out MemorySlot slot)
    {
        var slotSize = _layout.SlotSize;
        var newOffset = Interlocked.Add(ref _currentOffset, slotSize);

        if (newOffset > _poolMemory.Length)
        {
            slot = default;
            return false;
        }

        var startOffset = (int)(newOffset - slotSize);
        var slotSpan = _poolMemory.Span.Slice(startOffset, slotSize);
    
        slot = new MemorySlot(slotSpan, _layout);
        return true;
    }

    /// <summary>
    /// Dato un puntatore a un nodo, restituisce la "vista" MemorySlot corrispondente.
    /// </summary>
    public unsafe MemorySlot GetSlotFromPointer(Node* nodePtr)
    {
        var slotSpan = new Span<byte>(nodePtr, _layout.SlotSize);
        return new MemorySlot(slotSpan, _layout);
    }
    
    /// <summary>
    /// Riconfigura il pool con i parametri di una nuova partita.
    /// </summary>
    public void Reset(in MemoryLayout layout)
    {
        _layout = layout;
        _currentOffset = 0;
    }
    
    public void Reset(MemoryLayout layout) => _layout = layout;
    
    // Restituisce la memoria al pool condiviso quando il nostro pool viene eliminato.
    public void Dispose() => _memoryOwner.Dispose();
}