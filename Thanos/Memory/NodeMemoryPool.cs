using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Thanos.Abstract;
using Thanos.Common;
using Thanos.MCST;

namespace Thanos.Memory;

public sealed unsafe class NodeMemoryPool : INodeMemoryPool
{
    private readonly byte* _basePointer;
    
    private readonly int _stride;
    private readonly byte _firstIndex;

    public int Count { get; private set; }
    public uint Capacity { get; }

    public NodeMemoryPool(uint capacity, byte firstIndex, in NodeMemoryLayout layout)
    {
        _stride = layout.Node.Length;
        _firstIndex = firstIndex;

        Count = _firstIndex;
        Capacity = capacity;
        
        var totalSize = (nuint)(capacity * _stride);
        
        var alignedTotalSize = (nuint)((int)totalSize).AlignUp64();

        _basePointer = (byte*)NativeMemory.AlignedAlloc(alignedTotalSize, Constants.CacheLine);
        
        NativeMemory.Clear(_basePointer, alignedTotalSize);
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