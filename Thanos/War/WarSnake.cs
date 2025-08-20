using System.Runtime.CompilerServices;
using Thanos.SourceGen;

namespace Thanos.War;

public ref struct WarSnake
{
    private ref WarSnakeHeader _header;
    private readonly Span<ushort> _body;

    /// <summary>
    /// COSTRUTTORE 1 (inizializza la memoria grezza):
    /// 1. Si collega alla memoria grezza (header e body).
    /// 2. Inizializza quella memoria usando i dati forniti (snake, body).
    /// </summary>
    public WarSnake(ref WarSnakeHeader header, in Snake snake, Span<ushort> body, ReadOnlySpan<ushort> body1D, int capacity)
    {
        // Fase 1: Collegamento alla memoria
        _header = ref header;
        _body = body;
        
        // Fase 2: Inizializzazione della memoria
        body1D.CopyTo(_body);
        
        var length = _body.Length;
        _header = new WarSnakeHeader(0, snake.Health, capacity, length, _body[length - 1], length & (capacity - 1), 0);
    }
    
    /// <summary>
    /// COSTRUTTORE 2 (per la "vista"):
    /// Si collega semplicemente alla memoria già inizializzata.
    /// </summary>
    public WarSnake(ref WarSnakeHeader header, Span<ushort> body)
    {
        _header = ref header;
        _body = body;
    }

    public WarSnakeHeader Header => _header;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Move(ushort newHead, bool hasEaten, int damage)
    {
        var alive = hasEaten
            ? _header.FullCure()
            : _header.Damage(damage);
        
        if (alive) return;
        
        _body[_header.NextHeadIndex] = newHead;
        _header.PushHead(newHead);
        
        if (hasEaten) 
            _header.IncrementLength(); 
        else 
            _header.PopTail();
    }
    
    public readonly void GetSpans(out Span<ushort> first, out Span<ushort> second)
    {
        var tailIndex = _header.TailIndex;
        var length = _header.Length;
        var capacity = _header.Capacity;

        if (length == 0)
        {
            first = Span<ushort>.Empty;
            second = Span<ushort>.Empty;
            return;
        }

        var headIndex = (tailIndex + length - 1) & (capacity - 1);

        if (tailIndex <= headIndex)
        {
            first = _body.Slice(tailIndex, length);
            second = Span<ushort>.Empty;
        }
        else
        {
            var firstLength = capacity - tailIndex;
            first = _body.Slice(tailIndex, firstLength);
            second = _body[..(length - firstLength)];
        }
    }
    
    [StructLayout(LayoutKind.Sequential)]
    public struct WarSnakeHeader(int index, int health, int capacity, int length, ushort head, int nextHeadIndex, int tailIndex)
    {
        public int Index { get; } = index;
        public int Health { get; private set; } = health;
        public int Capacity { get; } = capacity;
        public int Length { get; private set; } = length;
        public ushort Head { get; private set; } = head;
        public int NextHeadIndex { get; private set; } = nextHeadIndex;
        public int TailIndex { get; private set; } = tailIndex;

        public void Kill() => Health = 0;

        public bool Damage(int amount)
        {
            Health -= amount;
            return Dead;
        }

        public bool FullCure()
        {
            Health = 100;
            return true;
        }

        public readonly bool Dead => Health <= 0;

        public void PushHead(ushort newHead)
        {
            Head = newHead;
            NextHeadIndex = (NextHeadIndex + 1) & (Capacity - 1);
        }

        public void PopTail() => TailIndex = (TailIndex + 1) & (Capacity - 1);

        public void IncrementLength()
        {
            if (Length < Capacity) Length++;
        }
    }
}