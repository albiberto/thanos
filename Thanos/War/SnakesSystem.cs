using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Thanos.Memory;
using Thanos.Memory.Pools;

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
        var headerOffset = _layout.GetSnakeHeaderOffset(index);
        var headerMemory = _memory.Slice(headerOffset, _layout.HeaderStride);
            
        ref var headerBaseRef = ref MemoryMarshal.GetReference(headerMemory);
        ref var health = ref Unsafe.As<byte, SnakeHealth>(ref headerBaseRef);
        ref var anatomy = ref Unsafe.As<byte, SnakeAnatomy>(ref Unsafe.Add(ref headerBaseRef, _layout.SizeOfHealth));

        var bitboardOffset = _layout.GetSnakeBitboardOffset(index);
        var bitboardByteSpan = _memory.Slice(bitboardOffset, _layout.BitboardSize);
        var bitboard = new Bitboard(bitboardByteSpan);
            
        return new WarSnake(health, anatomy, bitboard);
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