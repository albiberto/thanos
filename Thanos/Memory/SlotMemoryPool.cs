using System.Runtime.InteropServices;
using Thanos.PreWarm.Memory;

namespace Thanos.Memory;

public sealed unsafe class SlotMemoryPool : IDisposable
{
    private GameContext _context;
    
    private readonly byte* _basePointer;
    
    private readonly int _slotSize;
    private readonly long _maxNodes;
    private readonly nuint _totalSize;

    public SlotMemoryPool(in GameContext context, long maxNodes)
    {
        _context = context;
        _slotSize = context.Layout.WarSlotSize;
        _maxNodes = maxNodes;

        _totalSize = (nuint)(_slotSize * maxNodes);
        
        _basePointer = (byte*)NativeMemory.AlignedAlloc(_totalSize, 64);
        NativeMemory.Clear(_basePointer, _totalSize);
        
        Console.WriteLine($"[SlotMemoryPool] Allocated {(double)_totalSize / (1024 * 1024 * 1024):F3} GB for {_slotSize}-byte slots, max nodes: {maxNodes}");
    }
    
    public MemorySlot this[int index]
    {
        get
        {
            // --- 1. PROTEZIONE DELLA MEMORIA ---
            if (index >= _maxNodes) throw new OutOfMemoryException($"Accesso illegale allo SlotMemoryPool. Richiesto indice {index}, ma la capacità massima è {_maxNodes}.");
            
            // Console.WriteLine($"[SlotMemoryPool] Allocated {(double)(_slotSize * index) / (1024 * 1024 * 1024):F3} GB for {_slotSize}-byte slots, current node: {index}, max nodes: {_maxNodes}");
            
            // --- 2. CALCOLO DEL PUNTATORE ---
            var startOffset = (long)index * _slotSize;
            var slotPointer = _basePointer + startOffset;
            var slotSpan = new Span<byte>(slotPointer, _slotSize);
            return new MemorySlot(slotSpan, ref _context);
        }
    }

    public void Dispose() => NativeMemory.Free(_basePointer);

    public void Set(in GameContext context) => _context = context;
}