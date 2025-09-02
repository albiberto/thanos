using System.Buffers;
using System.Runtime.InteropServices;
using Thanos.MCST;
using Thanos.MCST.Memory;

namespace Thanos.Memory;

public sealed class NodeMemoryPool : IDisposable
{
    private readonly NodeMemoryLayout _layout;
    private readonly Memory<byte> _memory;

    private readonly IMemoryOwner<byte> _memoryOwner;
    private MemoryHandle _memoryHandle;

    public NodeMemoryPool(in NodeMemoryLayout layout, int maxNodes)
    {
        _layout = layout;

        _memoryOwner = MemoryPool<byte>.Shared.Rent(_layout.Size * maxNodes * 10);

        _memory = _memoryOwner.Memory;
        _memory.Span.Clear();
        _memoryHandle = _memory.Pin();
    }

    public ref Node this[int index]
    {
        get
        {
            var memory = _memory.Span.Slice(index * _layout.Size, _layout.Size);
            return ref MemoryMarshal.GetReference(MemoryMarshal.Cast<byte, Node>(memory));
        }
    }

    public void Dispose()
    {
        _memoryHandle.Dispose(); // 1. Prima rilascia l'handle
        _memoryOwner.Dispose(); // 2. Poi restituisci la memoria
    }

    public void Clear()
    {
        _memory.Span.Clear();
    }
}