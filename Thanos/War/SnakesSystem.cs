using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Thanos.Memory;

namespace Thanos.War;

public readonly ref struct SnakesSystem
{
    private readonly Span<byte> _memory;
    
    private readonly ref readonly MemoryLayout _layout;

    public SnakesSystem(Span<byte> memory, in MemoryLayout layout, int count)
    {
        _memory = memory;
        
        _layout = ref layout;
        
        Count = count;
    }

    public int Count { get; }

    public WarSnake Me => this[0];
    public Enumerator Enemies => new(this, 1);
    
    public WarSnake this[int index] => Build(index);

    private WarSnake Build(int index)
    {
        // 1. Trova l'Header del serpente.
        var headerOffset = _layout.GetSnakeHeaderOffset(index);
        var headerMemory = _memory.Slice(headerOffset, _layout.HeaderStride);
        
        // 2. Ora otteniamo un singolo riferimento all'header completo.
        ref var headerBaseRef = ref MemoryMarshal.GetReference(headerMemory);
        ref var header = ref Unsafe.As<byte, WarSnakeHeader>(ref headerBaseRef);

        // 3. Trova il Bitboard.
        var bitboardOffset = _layout.GetSnakeBitboardOffset(index);
        var bitboardByteSpan = _memory.Slice(bitboardOffset, _layout.BitboardSize);
            
        // 4. Il costruttore di WarSnake
        return new WarSnake(ref header, bitboardByteSpan);
    }
    
    public ref struct Enumerator(SnakesSystem system, int start)
    {
        private readonly SnakesSystem _system = system;
        private readonly int _count = system.Count;
        
        private int _index = start -1;

        public bool MoveNext()
        {
            _index++;
            return _index < _count;
        }

        public WarSnake Current => _system.Build(_index);
    }
}