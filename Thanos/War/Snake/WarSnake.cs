using System.Runtime.CompilerServices;

namespace Thanos.War.Snake;

public ref struct WarSnake
{
    private ref Profile _profile;
    private ref Anatomy _anatomy;
    private readonly Span<ushort> _body;

    // Costruttore principale
    public WarSnake(ref Profile profile, ref Anatomy anatomy, int id, int health, Span<ushort> body, ReadOnlySpan<ushort> body1D, int capacity)
    {
        _profile = ref profile;
        _profile = new Profile(id, health);
        
        _anatomy = ref anatomy;
        _anatomy = new Anatomy(capacity, body1D.Length, 0);
        
        _body = body;
        body1D.CopyTo(_body);
    }
    
    public WarSnake(ref Profile profile, ref Anatomy anatomy, Span<ushort> body)
    {
        _profile = ref profile;
        _anatomy = ref anatomy;
        
        _body = body;
    }

    // --- Proprietà ---
    // Ora è WarSnake che legge da _body usando gli indici di Anatomy
    public Profile Profile => _profile;
    public readonly ushort Head => _body[_anatomy.HeadIndex];
    public readonly ushort Tail => _body[_anatomy.TailIndex];
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Move(ushort newHead, bool hasEaten, int damage)
    {
        if (hasEaten)
            _profile.FullCure();
        else
            _profile.Damage(damage);

        if (_profile.Dead) return;
        
        // 1. WarSnake scrive il nuovo valore nel corpo
        _body[_anatomy.NextHeadIndex] = newHead;

        // 2. WarSnake dice ad Anatomy di aggiornare il suo stato
        if (hasEaten)
            _anatomy.IncrementLength();
        else
            _anatomy.PopTail();
    }
    
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