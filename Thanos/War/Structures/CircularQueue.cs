using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Thanos.War.Structures;

public ref struct CircularQueue(Span<byte> raw, ref CircularQueueState state)
{
    public Span<byte> Raw { get; } = raw;
    private Span<ushort> Buffer { get; } = MemoryMarshal.Cast<byte, ushort>(raw);

    private ref CircularQueueState _state = ref state;

    public ushort PeekHead => Buffer[(_state.HeadIndex - 1) & _state.WrapMask];
    public ushort PeekTail => Buffer[_state.TailIndex];
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
}