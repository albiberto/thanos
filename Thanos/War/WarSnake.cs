using System.Runtime.CompilerServices;

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
    public WarSnake(ref WarSnakeHeader header, int id, int health, Span<ushort> body, ReadOnlySpan<ushort> body1D, int capacity)
    {
        // Fase 1: Collegamento alla memoria
        _header = ref header;
        _body = body;
        
        // Fase 2: Inizializzazione della memoria
        body1D.CopyTo(_body);
        
        var length = _body.Length;
        var head = _body[length - 1];
        _header = new WarSnakeHeader(id, head, health, length, capacity);
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
        var dead = hasEaten
            ? _header.FullCure()
            : _header.Damage(damage);
        
        if (dead) return;
        
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
}