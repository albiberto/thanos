using System.Runtime.InteropServices;
using Thanos.MCST;
using Thanos.MCST.Memory;

namespace Thanos.Memory;

// La classe deve essere 'unsafe' per usare i puntatori
public sealed unsafe class NodeMemoryPool : IDisposable
{
    private readonly NodeMemoryLayout _layout;
    
    private readonly byte* _basePointer;

    private readonly int _slotSize;
    private readonly int _maxNodes;
    private readonly long _totalSize;

    public NodeMemoryPool(in NodeMemoryLayout layout, int maxNodes)
    {
        _layout = layout;
        _slotSize = layout.Size;
        _maxNodes = maxNodes;
        
        _totalSize = (long)_layout.Size * maxNodes * 10;

        _basePointer = (byte*)NativeMemory.AlignedAlloc((nuint)_totalSize, 64);
        NativeMemory.Clear(_basePointer, (nuint)_totalSize);
        
        Console.WriteLine($"[NodeMemoryPool] Allocated {(double)_totalSize / (1024 * 1024 * 1024):F3} GB for {_layout.Size}-byte nodes, max nodes: {_maxNodes}");
        
    }

    public ref Node this[int index]
    {
        get
        {
            // --- 1. PROTEZIONE DELLA MEMORIA ---
            if (index >= _maxNodes) throw new OutOfMemoryException($"Accesso illegale allo SlotMemoryPool. Richiesto indice {index}, ma la capacità massima è {_maxNodes}.");
            
            // Console.WriteLine($"[NodeMemoryPool] Allocated {(double)(_slotSize * index) / (1024 * 1024):F3} MB for {_slotSize}-byte slots, current node: {index}, max nodes: {_maxNodes}");

            // --- 2. CALCOLO DEL PUNTATORE ---
            var startOffset = (long)index * _layout.Size;
            var nodePointer = _basePointer + startOffset;
            var memorySpan = new Span<byte>(nodePointer, _layout.Size);
            
            return ref MemoryMarshal.GetReference(MemoryMarshal.Cast<byte, Node>(memorySpan));
        }
    }

    public void Dispose() => NativeMemory.Free(_basePointer);
}