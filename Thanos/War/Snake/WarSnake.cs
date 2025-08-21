using System.Runtime.CompilerServices;

namespace Thanos.War.Snake;

public ref struct WarSnake
{
    // Private fields
    private ref Health _health;
    private ref Anatomy _anatomy;
    
    private readonly Span<ushort> _body;

    // Costruttore principale: Inizializza la memoria grezza
    public WarSnake(ref Health health, ref Anatomy anatomy, Span<ushort> body, int id, int hp, ReadOnlySpan<ushort> body1D, int capacity)
    {
        Id = id;

        _health = ref health;
        _health = new Health(hp);

        _anatomy = ref anatomy;
        _anatomy = new Anatomy(capacity, body1D.Length);

        _body = body;
        body1D.CopyTo(_body);
    }
    
    // Costruttore alternativo: Inizializza la vista (ref struct)
    public WarSnake(ref Health health, ref Anatomy anatomy, Span<ushort> body)
    {
        _health = ref health;
        _anatomy = ref anatomy;

        _body = body;
    }
    
    // Public API
    public int Id { get; }

    public readonly ushort Head => _body[_anatomy.HeadIndex];
    public readonly ushort Tail => _body[_anatomy.TailIndex];
    public readonly int Length => _anatomy.Length;
    public readonly bool Dead => _health.Dead;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Move(ushort newHead, bool hasEaten, int damage)
    {
        if (hasEaten)
            _health.FullCure();
        else
            _health.Damage(damage);

        if (_health.Dead) return;

        // 1. WarSnake scrive il nuovo valore nel corpo
        _body[_anatomy.NextHeadIndex] = newHead;

        // 2. WarSnake dice ad Anatomy di aggiornare il suo stato
        if (hasEaten)
            _anatomy.IncrementLength();
        else
            _anatomy.PopTail();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Kill() => _health.Kill();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void GetSpans(out Span<ushort> first, out Span<ushort> second)
    {
        var tailIndex = _anatomy.TailIndex;
        var capacity = _anatomy.Capacity;
        var length = _anatomy.Length; // Legge la lunghezza da Anatomy

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