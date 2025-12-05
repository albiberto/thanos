using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Thanos.Abstract;
using Thanos.MCST;

namespace Thanos.Memory;

public sealed unsafe class NodeMemoryPool : INodePool
{
    private readonly byte* _basePointer;
    
    private readonly int _stride;
    private readonly byte _firstIndex;

    public int Count { get; private set; }
    public uint Capacity { get; }

    public NodeMemoryPool(uint capacity, byte firstIndex, in NodeMemoryLayout layout)
    {
        _stride = layout.Size;
        _firstIndex = firstIndex;

        Count = _firstIndex;
        Capacity = capacity;
        
        var totalSize = (nuint)(capacity * _stride);
        _basePointer = (byte*)NativeMemory.AlignedAlloc(totalSize, Constants.CacheLine);
        
        NativeMemory.Clear(_basePointer, totalSize);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref Node Get(int index) => ref Unsafe.AsRef<Node>(_basePointer + ((long)index * _stride));

    public ref Node this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref Get(index);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Allocate()
    {
        if (Count >= Capacity) return -1;
        return Count++;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Reset() => Count = _firstIndex;

    public void Dispose() => NativeMemory.AlignedFree(_basePointer);
}