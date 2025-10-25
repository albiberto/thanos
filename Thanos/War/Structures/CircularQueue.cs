using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Thanos.War.Structures;

public ref struct CircularQueue(Span<byte> raw, WarSnakeAnatomy anatomy)
{
    public Span<byte> Raw { get; } = raw;
    private readonly Span<ushort> _memory = MemoryMarshal.Cast<byte, ushort>(raw);
    
    private WarSnakeAnatomy _anatomy = anatomy;

    public ushort PeekHead => _memory[(_anatomy.HeadIndex - 1) & _anatomy.CapacityMask];
    public ushort PeekTail => _memory[_anatomy.TailIndex];
    public readonly int Length => _anatomy.Length;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Enqueue(ushort value)
    {
        _memory[_anatomy.HeadIndex] = value;
        _anatomy.IncrementHead();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort Dequeue()
    {
        var value = _memory[_anatomy.TailIndex];

        _anatomy.IncrementTail();
        
        return value;
    }
}