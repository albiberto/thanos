using System.Runtime.CompilerServices;
using Thanos.War.Snake.Memory;

namespace Thanos.War.Snake;

public readonly ref struct WarSnakes(Span<byte> headersMemory, Span<byte> bodiesMemory, in WarSnakeMemoryLayout layout, int count)
{
    private readonly Span<byte> _headersMemory = headersMemory;
    private readonly Span<byte> _bodiesMemory = bodiesMemory;
    private readonly ref WarSnakeMemoryLayout _layout = ref layout;

    public WarSnake Me => this[0];
    
    public WarSnake this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            var view = new WarSnakeMemoryView(_headersMemory, _bodiesMemory, in _layout, index);
            return new(view);
        }
    }

    public Enumerator Enemies => new(_headersMemory, _bodiesMemory, in _layout, 1, count);
    public Enumerator All => new(_headersMemory, _bodiesMemory, in _layout, 0, count);

    public ref struct Enumerator(Span<byte> headersMemory, Span<byte> bodiesMemory, in WarSnakeMemoryLayout layout, int start, int count)
    {
        private readonly Span<byte> _headersMemory = headersMemory;
        private readonly Span<byte> _bodiesMemory = bodiesMemory;
        private readonly ref WarSnakeMemoryLayout _layout = ref layout;
        private readonly int _count = count;
        
        private int _index = start -1;

        public bool MoveNext()
        {
            _index++;
            return _index < _count;
        }

        public WarSnake Current
        {
            get
            {
                var view = new WarSnakeMemoryView(_headersMemory, _bodiesMemory, in _layout, _index);
                return new WarSnake(view);
            }
        }
    }
}