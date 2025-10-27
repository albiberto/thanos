using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Thanos.Memory;
using Thanos.War.Structures;

namespace Thanos.War;

public ref struct SnakesSystem(Span<byte> raw, in SlotMemoryLayout layout, int count)
{
    public Span<byte> Raw { get; set; } = raw;
    private readonly ref readonly SlotMemoryLayout _layout = ref layout;
    
    public int Count { get; } = count;

    public WarSnake Me => this[0];
    public WarSnake this[int index] => Build(index);

    private WarSnake Build(int index)
    {
        var snakeBaseOffset = index * _layout.SnakeStride;
        var snakeMemory = Raw.Slice(snakeBaseOffset, _layout.SnakeStride);

        var lifeSpan = snakeMemory.Slice(_layout.WarSnakeLifeOffset, _layout.WarSnakeLifeSize);
        var anatomySpan = snakeMemory.Slice(_layout.CircularQueueStateOffset, _layout.CircularQueueStateSize);
        var bitboardSpan = snakeMemory.Slice(_layout.BitboardOffset, _layout.BitboardSize);
        var queueSpan = snakeMemory.Slice(_layout.QueueBufferOffset, _layout.QueueBufferSize);
        
        ref var life = ref Unsafe.As<byte, WarSnakeLife>(ref MemoryMarshal.GetReference(lifeSpan));
        ref var state = ref Unsafe.As<byte, CircularQueueState>(ref MemoryMarshal.GetReference(anatomySpan));
        state.PlacementNew(_layout.Capacity);
        
        bitboardSpan.Clear();
        var bitboard = new Bitboard(bitboardSpan);
        
        queueSpan.Clear();
        var queue = new CircularQueue(queueSpan, ref state);

        return new WarSnake(ref life, bitboard, queue);
    }
}