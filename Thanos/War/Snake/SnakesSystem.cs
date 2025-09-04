using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Thanos.Memory.Pools;
using Thanos.War.Grid;

namespace Thanos.War.Snake;

public readonly ref struct SnakesSystem
{
    private readonly Span<byte> _headersMemory;
    private readonly Span<ulong> _bitboardsMemory;
    
    private readonly ref readonly MemoryLayout _layout;

    public SnakesSystem(Span<byte> headersMemory, Span<ulong> bitboardsMemory, in MemoryLayout layout, int count)
    {
        _headersMemory = headersMemory;
        _bitboardsMemory = bitboardsMemory;
        
        _layout = ref layout;
        
        Count = count;
    }

    public int Count { get; }

    public WarSnake Me => this[0];
    public Enumerator Enemies => new(this, 1);
    
    public WarSnake this[int index] => Build(index);

    private WarSnake Build(int index)
    {
        var headerOffset = index * _layout.HeaderStride;
        var healthMemory = _headersMemory.Slice(headerOffset, _layout.HeaderStride);
        var anatomyMemory = healthMemory[_layout.SizeOfHealth..];
        
        var health = Unsafe.As<byte, Health>(ref MemoryMarshal.GetReference(healthMemory));
        var anatomy = Unsafe.As<byte, Anatomy>(ref MemoryMarshal.GetReference(anatomyMemory));
        
        var bitboardOffset = index * _layout.BitboardSize;
        var bitboardMemory = _bitboardsMemory.Slice(bitboardOffset, _layout.BitboardSize);

        var bitboard = new Bitboard(bitboardMemory);
        
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