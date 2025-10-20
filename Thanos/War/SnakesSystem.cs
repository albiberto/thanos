// Thanos/War/SnakesSystem.cs

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Thanos.Memory;

namespace Thanos.War;

public readonly ref struct SnakesSystem
{
    private readonly Span<byte> _memory;
    private readonly ref readonly PoolMemoryLayout _layout;

    public SnakesSystem(Span<byte> memory, in PoolMemoryLayout layout, int count)
    {
        _memory = memory;
        _layout = ref layout;
        Count = count;
    }

    public int Count { get; }
    public Span<byte> Raw => _memory;
    public WarSnake Me => this[0];
    public WarSnake this[int index] => Build(index);

    private WarSnake Build(int index)
    {
        // 1. Trova l'Header del serpente (invariato)
        var headerOffset = _layout.HeadersBaseOffset + index * _layout.HeaderStride;
        var headerMemory = _memory.Slice(headerOffset, _layout.HeaderStride);
        ref var headerBaseRef = ref MemoryMarshal.GetReference(headerMemory);
        ref var header = ref Unsafe.As<byte, WarSnakeHeader>(ref headerBaseRef);

        // 2. Trova il Bitboard (invariato)
        var bitboardOffset = _layout.BitboardOffsets[LayoutConstants.GlobalBitboardCount + index];
        var bitboardByteSpan = _memory.Slice(bitboardOffset, _layout.BitboardSize);

        // 3. NUOVO: Trova il Buffer Circolare
        var bufferOffset = _layout.CircularBuffersBaseOffset + index * _layout.CircularBufferStride;
        // La dimensione del buffer per un singolo serpente è lo stride
        var bufferByteSpan = _memory.Slice(bufferOffset, _layout.CircularBufferStride); 
        // Converte lo span di byte in uno span del tipo corretto (ushort)
        var bufferUshortSpan = MemoryMarshal.Cast<byte, ushort>(bufferByteSpan);

        // 4. Chiama il nuovo costruttore di WarSnake con tutti i pezzi di memoria
        return new WarSnake(ref header, bitboardByteSpan, bufferUshortSpan, _layout.Capacity);
    }
}