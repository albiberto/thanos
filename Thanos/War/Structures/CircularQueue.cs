using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Thanos.War.Structures;

public ref struct CircularQueue
{
    public readonly Span<byte> Raw;
    public readonly Span<ushort> Buffer;
    
    private ref CircularQueueState _state;
    private readonly int _mask;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public CircularQueue(Span<byte> raw, ref CircularQueueState state, ushort capacity)
    {
        Raw = raw;
        Buffer = MemoryMarshal.Cast<byte, ushort>(raw);

        _state = ref state;
        _mask = capacity - 1; 
    }
    
    public readonly ushort PeekHead
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Buffer[(_state.HeadIndex - 1) & _mask];
    }

    public readonly ushort PeekTail
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Buffer[_state.TailIndex & _mask];
    }
    
    public readonly ushort PeekElementBeforeTail
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Buffer[(_state.TailIndex + 1) & _mask];
    }
    
    public readonly int Length 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _state.Length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Enqueue(ushort value)
    {
        Buffer[_state.HeadIndex & _mask] = value;
        
        _state.HeadIndex++;
        _state.Length++;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort Dequeue()
    {
        var value = Buffer[_state.TailIndex & _mask];
        
        _state.TailIndex++;
        _state.Length--;
        
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear() => _state.Reset();
}