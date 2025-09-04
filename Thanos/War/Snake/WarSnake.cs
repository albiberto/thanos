using System.Runtime.CompilerServices;
using Thanos.War.Snake.Memory;

namespace Thanos.War.Snake;

public readonly ref struct WarSnake
{
    // Riferimenti diretti ai componenti in memoria
    private readonly ref Health _health;
    private readonly ref Anatomy _anatomy;
    
    private readonly Span<ushort> _body;
    
    // Dati di configurazione
    private readonly int _capacity;
    private readonly int _capacityMask;

    public WarSnake(WarSnakeMemoryView view)
    {
        _health = ref view.Health;
        _anatomy = ref view.Anatomy;
        _body = view.Body;
        _capacity = view.BodyCapacity;
        _capacityMask = _capacity - 1;
    }

    // API Pubblica
    public int HP => _health.Points;
    public int Length => _anatomy.Length;
    public bool IsDead => _health.IsDead;

    public ushort Head => _body[(_anatomy.TailIndex + _anatomy.Length - 1) & _capacityMask];
    public ushort Tail => _body[_anatomy.TailIndex];
    private int NextHeadIndex => (_anatomy.TailIndex + _anatomy.Length) & _capacityMask;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Move(ushort newHead, bool hasEaten, byte damage)
    {
        // La logica di crescita viene decisa qui, a un livello più alto
        var willGrow = hasEaten && _anatomy.Length < _capacity;

        if (hasEaten)
            _health.FullCure();
        else
            _health.Damage(damage);

        // Se lo snake muore dopo aver subito danno, non si muove
        if (_health.IsDead) return;

        // La testa si muove sempre, scrivendo la nuova coordinata
        _body[NextHeadIndex] = newHead;

        // L'anatomia viene aggiornata in base alla crescita
        if (willGrow)
            _anatomy.UpdateAfterGrow();
        else
            _anatomy.UpdateAfterMove(_capacity);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Kill() => _health.Kill();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [SkipLocalsInit]
    public void GetSpans(out Span<ushort> first, out Span<ushort> second)
    {
        if (_anatomy.Length == 0)
        {
            first = second = Span<ushort>.Empty;
            return;
        }

        var headIndex = (_anatomy.TailIndex + _anatomy.Length - 1) & _capacityMask;

        if (_anatomy.TailIndex <= headIndex)
        {
            first = _body.Slice(_anatomy.TailIndex, _anatomy.Length);
            second = Span<ushort>.Empty;
        }
        else
        {
            var firstLength = _capacity - _anatomy.TailIndex;
            first = _body.Slice(_anatomy.TailIndex, firstLength);
            second = _body[..(headIndex + 1)];
        }
    }
}