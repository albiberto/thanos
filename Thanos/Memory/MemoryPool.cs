using System.Buffers;
using Thanos.MCST;
using Thanos.War;
// Necessario per IMemoryOwner

namespace Thanos.Memory;

public sealed class MemoryPool : IDisposable
{
    private readonly IMemoryOwner<byte> _memoryOwner;
    private readonly Memory<byte> _poolMemory;
    private long _currentOffset;
    
    private WarContext _context;
    private MemoryLayout _layout;

    public MemoryPool(in WarContext context, in MemoryLayout layout)
    {
        _context = context;
        _layout = layout;
        _memoryOwner = MemoryPool<byte>.Shared.Rent((int)layout.PoolSize);
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
            // Se non c'è spazio, il parametro 'out' deve essere inizializzato.
            slot = default;
            return false;
        }

        var startOffset = (int)(newOffset - slotSize);
        var slotSpan = _poolMemory.Span.Slice(startOffset, slotSize);
    
        // CORREZIONE: Crea l'istanza di MemorySlot e la assegna al parametro 'out'.
        slot = new MemorySlot(slotSpan, _context, _layout);
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
    
    /// <summary>
    /// Riconfigura il pool con i parametri di una nuova partita.
    /// </summary>
    public void Reset(in WarContext context, in MemoryLayout layout)
    {
        _context = context;
        _layout = layout;
        Reset(); // Chiama il reset dell'offset
    }
    
    // Restituisce la memoria al pool condiviso quando il nostro pool viene eliminato.
    public void Dispose() => _memoryOwner.Dispose();
}