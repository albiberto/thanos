using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Thanos.Abstract;
using Thanos.MCST;

namespace Thanos.Memory;

public sealed unsafe class NodeMemoryPool : INodeMemoryPool
{
    private readonly byte* _basePointer;

    private readonly int _firstIndex;
    private readonly nuint _stride; 
    private readonly NodeMemoryLayout _layout;

    public uint Capacity { get; }
    public int Index { get; private set; }

    public NodeMemoryPool(uint capacity, int firstIndex, in NodeMemoryLayout layout)
    {
        _firstIndex = firstIndex;

        _layout = layout;
        _stride = layout.Node.Next;
        
        Capacity = capacity;
        Index = _firstIndex;
        
        var totalSize = (nuint)capacity * _stride;
        _basePointer = (byte*)NativeMemory.AlignedAlloc(totalSize, Constants.CacheLine);
        NativeMemory.Clear(_basePointer, totalSize);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref Node Get(int index) => ref Unsafe.AsRef<Node>(_basePointer + (nuint)index * _stride);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Allocate()
    {
        if (Index >= Capacity) return -1;
        return Index++;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Reset() => Index = _firstIndex;

    public void Dispose() => NativeMemory.AlignedFree(_basePointer);
}