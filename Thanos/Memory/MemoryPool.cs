using System.Buffers;
using Thanos.Enums;
using Thanos.MCST;
using Thanos.War;
// Necessario per IMemoryOwner

namespace Thanos.Memory;

public sealed class MemoryPool : IDisposable
{
    private readonly IMemoryOwner<byte> _memoryOwner;
    private readonly Memory<byte> _memory;
    private MemoryHandle _memoryHandle;
    private long _currentOffset;
    
    // Il context e la mappa non sono più readonly, vengono impostati da Reset
    private GameContext _context; 
    private Dictionary<string, int> _snakeIdMap = [];

    // COSTRUTTORE SEMPLIFICATO: alloca solo la memoria
    public MemoryPool(in GameContext worstContext, int maxNodes)
    {
        _context = worstContext;
        
        _memoryOwner = MemoryPool<byte>.Shared.Rent(worstContext.Layout.SlotSize * maxNodes);
        _memory = _memoryOwner.Memory;
        _memoryHandle = _memory.Pin();
    }

    // RESET: configura il pool per una partita specifica
    public void Reset(in GameContext context, Dictionary<string, int> snakeIdMap)
    {
        _context = context;
        _snakeIdMap = snakeIdMap;
        _currentOffset = 0;
    }

    /// <summary>
    /// Tenta di ottenere il prossimo slot di memoria e restituisce la vista MemorySlot già pronta.
    /// </summary>
    public bool TryGetNext(out MemorySlot slot)
    {
        var slotSize = _context.Layout.SlotSize;
        var newOffset = Interlocked.Add(ref _currentOffset, slotSize);

        if (newOffset > _memory.Length)
        {
            slot = default;
            return false;
        }

        var startOffset = (int)(newOffset - slotSize);
        var slotSpan = _memory.Span.Slice(startOffset, slotSize);
    
        slot = new MemorySlot(slotSpan, in _context, _snakeIdMap);
        return true;
    }

    /// <summary>
    /// Dato un puntatore a un nodo, restituisce la "vista" MemorySlot corrispondente.
    /// </summary>
    public unsafe MemorySlot GetSlotFromPointer(Node* nodePtr)
    {
        var slotSpan = new Span<byte>(nodePtr, _context.Layout.SlotSize);
        return new MemorySlot(slotSpan, in _context, _snakeIdMap);
    }
    
    /// <summary>
    /// Riconfigura il pool con i parametri di una nuova partita.
    /// </summary>
    public void Reset(in GameContext context)
    {
        _context = context;
        _currentOffset = 0;
    }
    
    public void Reset(GameContext context) => _context = context;
    
    // Restituisce la memoria al pool condiviso quando il nostro pool viene eliminato.
    public void Dispose()
    {
        _memoryOwner.Dispose();
        _memoryHandle.Dispose();
    }
}