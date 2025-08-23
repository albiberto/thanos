using System.Runtime.CompilerServices;

namespace Thanos.War.Snake;

public readonly ref struct WarSnake
{
    // Private fields
    private readonly ref Profile _profile;
    private readonly ref Health _health;
    private readonly ref Anatomy _anatomy;
    
    private readonly Span<ushort> _body;

    public WarSnake(WarSnakeMemoryView view){}
    
    public WarSnake(ref Profile profile, ref Health health, ref Anatomy anatomy, Span<ushort> body)
    {
        _profile = ref profile;
        _health = ref health;
        _anatomy = ref anatomy;
        
        _body = body;
    }
    
    // Public API
    public int Id => _profile.Id;

    public ushort Head => _body[_anatomy.HeadIndex];
    public ushort Tail => _body[_anatomy.TailIndex];
    public int Length => _anatomy.Length;
    public bool Dead => _health.IsDead;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [SkipLocalsInit]
    public void Move(ushort newHead, bool hasEaten, int damage)
    {
        if (hasEaten)
            _health.FullCure();
        else
            _health.Damage(damage);

        if (_health.IsDead) return;

        _body[_anatomy.NextHeadIndex] = newHead;

        if (hasEaten)
            _anatomy.IncrementLength();
        else
            _anatomy.PopTail();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Kill() => _health.Kill();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [SkipLocalsInit]
    public void GetSpans(out Span<ushort> first, out Span<ushort> second)
    {
        var tailIndex = _anatomy.TailIndex;
        var capacity = _anatomy.Capacity;
        var capacityMask = _anatomy.CapacityMask;
        var length = _anatomy.Length; // Legge la lunghezza da Anatomy

        if (length == 0)
        {
            first = Span<ushort>.Empty;
            second = Span<ushort>.Empty;
            return;
        }

        var headIndex = (tailIndex + length - 1) & capacityMask;

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