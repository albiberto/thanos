using System.Buffers;
using System.Runtime.InteropServices;
using System.Threading;
using Thanos.MCST;
using Thanos.MCST.Memory;

namespace Thanos.Memory;

public sealed class NodeMemoryPool : IDisposable
{
    private readonly NodeMemoryLayout _layout;
    
    private readonly IMemoryOwner<byte> _memoryOwner;
    private readonly Memory<byte> _memory;
    private MemoryHandle _memoryHandle;
    
    public int _offset;

    public NodeMemoryPool(in NodeMemoryLayout layout, int maxNodes)
    {
        _layout = layout;
        
        _memoryOwner = MemoryPool<byte>.Shared.Rent(_layout.Size * maxNodes * 10);
        
        _memory = _memoryOwner.Memory;
        _memory.Span.Clear();
        _memoryHandle = _memory.Pin();
    }
    
    public int GetNextIndex()
    {
        var index = Interlocked.Increment(ref _offset) - 1;
        
        return index;
    }
    
    public ref Node this[int index]
    {
        get
        {
            var memory = _memory.Span.Slice(index * _layout.Size, _layout.Size);
            return ref MemoryMarshal.GetReference(MemoryMarshal.Cast<byte, Node>(memory));
        }
    }

    public void Clear()
    {
        _memory.Span.Clear();
        _offset = 0;
    }
    
    public void Reset() => _offset = 0;

    public void Dispose()
    {
        _memoryHandle.Dispose(); // 1. Prima rilascia l'handle
        _memoryOwner.Dispose();  // 2. Poi restituisci la memoria
    }
}