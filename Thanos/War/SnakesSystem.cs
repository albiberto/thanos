using System.Runtime.CompilerServices;
using Thanos.Memory;
using Thanos.War.Structures;

namespace Thanos.War;

public readonly ref struct SnakesSystem(Span<byte> raw, in SlotMemoryLayout layout, int count)
{
    public Span<byte> Raw { get; } = raw;
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
        // Calcoliamo l'offset di base per questo specifico serpente
        // SnakeStride è già allineato a 64 byte nel Layout
        var baseOffset = index * _layout.SnakeStride;

        // 1. Life (Accesso diretto unsafe al singolo byte/struct)
        // Offset = Base + OffsetRelativoLife
        ref var life = ref Unsafe.As<byte, WarSnakeLife>(ref Raw[baseOffset + _layout.WarSnakeLife.Offset]);

        // 2. Queue State (Accesso diretto unsafe)
        ref var state = ref Unsafe.As<byte, CircularQueueState>(ref Raw[baseOffset + _layout.CircularQueueState.Offset]);
        
        // 3. Bitboard (Slice dello Span)
        // Length qui è in bytes (come definito nel Layout constructor)
        var bitboardSpan = Raw.Slice(baseOffset + _layout.Bitboard.Offset, _layout.Bitboard.Length);
        var bitboard = new Bitboard(bitboardSpan);

        // 4. Queue Buffer (Slice dello Span)
        // Attenzione: QueueBuffer.Length nel Layout è il numero di elementi (Capacity), 
        // ma Slice vuole i bytes.
        var queueByteSize = _layout.QueueCapacity * sizeof(ushort);
        
        var queueSpan = Raw.Slice(baseOffset + _layout.QueueBuffer.Offset, queueByteSize);
        
        // Passiamo Capacity esplicita per evitare che la Queue debba calcolarsela o leggerla dallo State
        var queue = new CircularQueue(queueSpan, ref state, _layout.QueueCapacity);

        return new WarSnake(ref life, bitboard, queue);
    }

    /// <summary>
    /// Inizializza lo stato dei serpenti (Reset Head/Tail/Length).
    /// Da chiamare una volta sola quando si alloca/resetta lo slot nel Pool.
    /// </summary>
    public void Initialize()
    {
        for (var i = 0; i < Count; i++)
        {
            var baseOffset = i * _layout.SnakeStride;
            ref var state = ref Unsafe.As<byte, CircularQueueState>(ref Raw[baseOffset + _layout.CircularQueueState.Offset]);
            
            // Imposta Capacity e resetta i puntatori
            state.PlacementNew(_layout.QueueCapacity);
        }
    }
}