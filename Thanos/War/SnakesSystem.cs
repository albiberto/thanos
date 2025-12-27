using System.Runtime.CompilerServices;
using Thanos.Memory;
using Thanos.War.Structures;

namespace Thanos.War;

public readonly unsafe ref struct SnakesSystem(byte* basePtr, in SlotMemoryLayout layout, int count)
{
    private readonly byte* _basePtr = basePtr;
    private readonly ref readonly SlotMemoryLayout _layout = ref layout;

    public int Count { get; } = count;

    public WarSnake Me => this[0];

    public WarSnake this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Build(index);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private WarSnake Build(int index)
    {
        var snakePtr = _basePtr + ((nuint)index * _layout.SnakeStride.Next);

        ref var life = ref Unsafe.AsRef<WarSnakeLife>(snakePtr + _layout.WarSnakeLife.Offset);
        ref var state = ref Unsafe.AsRef<CircularQueueState>(snakePtr + _layout.CircularQueueState.Offset);
        
        var bitboardSpan = new Span<byte>(snakePtr + _layout.Bitboard.Offset, (int)_layout.Bitboard.Length);
        var queueSpan = new Span<byte>(snakePtr + _layout.QueueBuffer.Offset, (int)_layout.QueueBuffer.Length);
        
        return new WarSnake(ref life, new Bitboard(bitboardSpan), new CircularQueue(queueSpan, ref state, _layout.QueueCapacity));
    }

    public void Initialize()
    {
        for (var i = 0; i < Count; i++)
        {
            var snakePtr = _basePtr + ((nuint)i * _layout.SnakeStride.Next);
            ref var state = ref Unsafe.AsRef<CircularQueueState>(snakePtr + _layout.CircularQueueState.Offset);
            state.Reset();
        }
    }

    // --- NUOVO METODO: Copia raw ad altissima velocità ---
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopyFrom(in SnakesSystem source)
    {
        // Calcoliamo la dimensione totale: NumeroSerpenti * DimensioneStride (incluso padding)
        long totalSize = (long)Count * (long)_layout.SnakeStride.Next;
        
        // Memcopy brutale e veloce
        Buffer.MemoryCopy(source._basePtr, _basePtr, totalSize, totalSize);
    }
}