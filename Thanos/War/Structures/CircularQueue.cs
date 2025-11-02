using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Thanos.War.Structures;

public ref struct CircularQueue(Span<byte> raw, ref CircularQueueState state)
{
    public Span<byte> Raw { get; } = raw;
    public Span<ushort> Buffer { get; } = MemoryMarshal.Cast<byte, ushort>(raw);

    public ref CircularQueueState _state = ref state;

    public ushort PeekHead => Buffer[(_state.HeadIndex - 1) & _state.WrapMask];
    public ushort PeekTail => Buffer[_state.TailIndex];
    public ushort PeekElementBeforeTail => Buffer[(_state.TailIndex + 1) & _state.WrapMask];
    public readonly int Length => _state.Length;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Enqueue(ushort value)
    {
        Buffer[_state.HeadIndex] = value;
        _state.AdvanceHead();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort Dequeue()
    {
        var value = Buffer[_state.TailIndex];

        _state.AdvanceTail();

        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        Raw.Clear();
        _state.Reset();
    }
}