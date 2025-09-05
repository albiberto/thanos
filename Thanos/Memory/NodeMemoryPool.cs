using System.Runtime.InteropServices;
using Thanos.MCST;
using Thanos.MCST.Memory;

namespace Thanos.Memory;

public sealed unsafe class NodeMemoryPool : IDisposable
{
    private readonly byte* _basePointer;
    private readonly NodeMemoryLayout _layout;
    private readonly int _maxNodes;

    public NodeMemoryPool(in NodeMemoryLayout layout, int maxNodes)
    {
        _layout = layout;
        _maxNodes = maxNodes;

        var totalSize = (long)_layout.Size * maxNodes;

        _basePointer = (byte*)NativeMemory.AlignedAlloc((nuint)totalSize, Constants.CacheLine);
        NativeMemory.Clear(_basePointer, (nuint)totalSize);

        Console.WriteLine($"[NodeMemoryPool] Allocated {(double)totalSize / (1024 * 1024 * 1024):F3} GB for {_layout.Size}-byte nodes, max nodes: {_maxNodes}");
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
            // ALTERNATIVA: teoricamente più performante perchè evita la creazione dello span
            // ma probabilmente il JIT traduce la versione con MemoryMarshal nello stesso codice macchina
            // return ref Unsafe.AsRef<Node>(nodePointer);
        }
    }

    public void Dispose() => NativeMemory.Free(_basePointer);
}