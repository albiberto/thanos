using System.Runtime.CompilerServices;

namespace Thanos.War;

public readonly unsafe ref struct WarSnakeArray(WarSnake* start, uint count, uint stride)
{
    public ref WarSnake this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            var snakePtr = (byte*)start + (index * stride);
            return ref Unsafe.AsRef<WarSnake>(snakePtr);
        }
    }

    public uint Length => count;

    public Enumerator GetEnumerator() => new(this);

    public ref struct Enumerator
    {
        private readonly WarSnakeArray _array;
        private int _index;

        public Enumerator(in WarSnakeArray array) { _array = array; _index = -1; }
        public bool MoveNext() => ++_index < _array.Length;
        public ref WarSnake Current => ref _array[_index];
    }
}