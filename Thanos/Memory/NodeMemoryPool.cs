using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Thanos.MCST;

namespace Thanos.Memory;

public sealed unsafe class NodeMemoryPool : IDisposable
{
    private readonly byte* _basePointer;
    private readonly NodeMemoryLayout _layout;
    private readonly uint _maxNodes;

    public NodeMemoryPool(uint maxNodes, in NodeMemoryLayout layout)
    {
        _layout = layout;
        _maxNodes = maxNodes;

        var totalSize = _layout.Size * maxNodes;

        _basePointer = (byte*)NativeMemory.AlignedAlloc((nuint)totalSize, Constants.CacheLine);
        NativeMemory.Clear(_basePointer, (nuint)totalSize);

        Console.WriteLine($"[NodeMemoryPool] Allocated {(double)totalSize / (1024 * 1024 * 1024):F3} GB for {_layout.Size}-byte nodes, max nodes: {_maxNodes}");
    }

    public ref Node this[int index]
    {
        get
        {
            if (index >= _maxNodes) throw new OutOfMemoryException($"Accesso illegale allo SlotMemoryPool. Richiesto indice {index}, ma la capacità massima è {_maxNodes}.");

            var startOffset = (long)index * _layout.Size;
            var nodePointer = _basePointer + startOffset;

            return ref Unsafe.AsRef<Node>(nodePointer);
        }
    }

    public void Dispose() => NativeMemory.Free(_basePointer);
}