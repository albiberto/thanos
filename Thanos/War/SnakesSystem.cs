using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Thanos.Memory;
using Thanos.War.Structures;

namespace Thanos.War;

public readonly ref struct SnakesSystem
{
    private readonly Span<byte> _raw;
    private readonly ushort _capacity;
    private readonly ref SlotMemoryLayout _layout;

    public SnakesSystem(Span<byte> raw, ref SlotMemoryLayout layout, ushort capacity, int count)
    {
        _raw = raw;
        _layout = ref layout;
        _capacity = capacity;
        
        Count = count;
    }

    public int Count { get; }

    public WarSnake Me => this[0];
    public WarSnake this[int index] => Build(index);

    private WarSnake Build(int index)
    {
        var snakeBaseOffset = index * _layout.SnakeStride;
        var snakeMemory = _raw.Slice(snakeBaseOffset, _layout.SnakeStride);

        // 2. Affetta il blocco del serpente nei suoi componenti
        var lifeSpan = snakeMemory.Slice(_layout.WarSnakeLifeOffset, _layout.WarSnakeLifeSize);
        var anatomySpan = snakeMemory.Slice(_layout.CircularQueueStateOffset, _layout.CircularQueueStateSize);
        var bitboardSpan = snakeMemory.Slice(_layout.BitboardOffset, _layout.BitboardSize);
        var queueSpan = snakeMemory.Slice(_layout.QueueBufferOffset, _layout.QueueBufferSize);
        
        // 3. Ottieni i riferimenti ai dati di STATO (usando Unsafe.As)
        ref var life = ref Unsafe.As<byte, WarSnakeLife>(ref MemoryMarshal.GetReference(lifeSpan));
        ref var anatomy = ref Unsafe.As<byte, WarSnakeAnatomy>(ref MemoryMarshal.GetReference(anatomySpan));
        anatomy.Initialize(_capacity);
        
        // 5. Costruisci l'orchestratore WarSnake passando tutti i pezzi
        bitboardSpan.Clear();
        var bitboard = new Bitboard(bitboardSpan);
        
        queueSpan.Clear();
        var queue = new CircularQueue(queueSpan, anatomy);
        
        return new WarSnake(life, bitboard, queue);
    }
}