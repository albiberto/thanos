using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Thanos.War.Structures;

namespace Thanos.War.Snake;

public ref struct WarSnake(ref WarSnakeLife life, Bitboard bitboard, CircularQueue queue)
{
    private ref WarSnakeLife _life = ref life;

    private readonly Bitboard _bitboard = bitboard;
    private CircularQueue _queue = queue;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Initialize(in SourceGen.Snake snakeData)
    {
        ReadOnlySpan<ushort> body = snakeData.Body;
        var totalLength = body.Length;

        if (totalLength == 0) return;

        _life.SetHp(snakeData.Health);

        ref var baseRef = ref MemoryMarshal.GetReference(body);

        // --- PHASE 1: STACK SCAN ---
        var distinctLength = totalLength;
        for (var i = totalLength - 1; i > 0; i--)
            if (Unsafe.Add(ref baseRef, i) == Unsafe.Add(ref baseRef, i - 1))
                distinctLength--;
            else
                break;

        // --- PHASE 2: GROWTH STATE ---
        var credits = (byte)(totalLength - distinctLength);
        _life.SetPendingGrowth(credits);

        // --- PHASE 3: MEMORY POPULATION ---
        for (var i = distinctLength - 1; i >= 0; i--)
        {
            var part = Unsafe.Add(ref baseRef, i);
            _queue.Enqueue(part);
            _bitboard.Set(part);
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void InitializeFrom(ushort part)
    {
        _life.SetHp(100);

        _life.SetPendingGrowth(2);

        _queue.Enqueue(part);
        _bitboard.Set(part);
    }

    // --- PROPRIETÀ E ACCESSORI ---
    public ushort Head => _queue.PeekHead;
    public ushort Tail => _queue.PeekTail;
    public ushort PreTail => _queue.PreTail;
    
    public int Length => _queue.Length;
    public int ActualLength => _queue.Length + _life.Credits;
    
    public Bitboard Body => _bitboard;
    
    public int Hp => _life.Hp;
    public bool IsDead => _life.IsDead;
    public bool IsGrowthPending => _life.IsGrowthPending;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UpdateAfterMove(ushort newHead, bool ateFood, int damage)
    {
        if (IsDead) return;

        // 1. CREDIT ACCRUAL (Food logic)
        // Il cibo fornisce un credito di crescita immediato.
        // Gestiamo questo PRIMA del movimento per unificare la logica.
        if (ateFood)
        {
            _life.FullCure();
            _life.ScheduleGrowth(); // +1 Credit
        }
        else
        {
            _life.Damage((byte)damage);
        }

        // 2. MOVEMENT RESOLUTION
        // Consumiamo un credito per "pagare" la permanenza della coda.
        // Se abbiamo crediti (dallo stack iniziale O dal cibo appena mangiato), non facciamo Dequeue.
        var growing = _life.ConsumePendingGrowth(); // -1 Credit if > 0

        if (!growing)
        {
            // Nessun credito: la coda avanza (rimuoviamo il vecchio tail)
            var oldTailPos = _queue.Dequeue();
            if (oldTailPos != _queue.PeekTail) _bitboard.Unset(oldTailPos);
        }

        _queue.Enqueue(newHead);
        _bitboard.Set(newHead);
    }

    public void Kill() => _life.Kill();
    public bool IsOnBody(ushort position) => _bitboard.IsSet(position);
}