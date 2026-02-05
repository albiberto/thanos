using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Thanos.SourceGen;
using Thanos.War.Structures;

namespace Thanos.War;

public ref struct WarSnake(ref WarSnakeLife life, Bitboard bitboard, CircularQueue queue)
{
    private ref WarSnakeLife _life = ref life;

    private readonly Bitboard _bitboard = bitboard;

    private CircularQueue _queue = queue;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Initialize(in Snake snakeData)
    {
        // Conversione Zero-Cost: Array -> Span.
        // Il JIT sa che snakeData.Body è un array, la creazione dello Span è istantanea.
        ReadOnlySpan<ushort> body = snakeData.Body;
        var totalLength = body.Length;

        if (totalLength == 0) return;

        _life.SetHp(snakeData.Health);

        // Otteniamo un riferimento gestito (ref) al primo elemento.
        // Questo bypassa TOTALMENTE i controlli sui limiti (Bounds Checks) nei loop successivi.
        ref var baseRef = ref MemoryMarshal.GetReference(body);

        // --- PHASE 1: STACK SCAN ---
        // Identifichiamo dove finisce il corpo "vero" e inizia lo stack della coda.
        var distinctLength = totalLength;

        // Loop inverso ottimizzato
        for (var i = totalLength - 1; i > 0; i--)
            // Unsafe.Add(ref baseRef, i) è equivalente a baseRef[i] ma SENZA controlli.
            // Confrontiamo elemento corrente (i) con il precedente (i-1)
            if (Unsafe.Add(ref baseRef, i) == Unsafe.Add(ref baseRef, i - 1))
                distinctLength--;
            else
                // Appena troviamo una differenza, lo stack è finito.
                break;

        // --- PHASE 2: GROWTH STATE ---
        var credits = (byte)(totalLength - distinctLength);

        // Applichiamo la crescita pendente
        // (Nota: se WarSnakeLife avesse AddGrowth(int), sarebbe meglio del loop)
        _life.SetPendingGrowth(credits);

        // --- PHASE 3: MEMORY POPULATION ---
        // Inseriamo SOLO i segmenti unici.
        // Iteriamo all'indietro per rispettare l'ordine della Queue (Head ultima ad entrare).
        for (var i = distinctLength - 1; i >= 0; i--)
        {
            // Accesso diretto ultra-veloce
            var part = Unsafe.Add(ref baseRef, i);

            _queue.Enqueue(part);
            _bitboard.Set(part);
        }
    }

    // --- PROPRIETÀ E ACCESSORI ---
    public ushort Head => _queue.PeekHead;
    public ushort Tail => _queue.PeekTail;
    public ushort PreTail => _queue.PreTail;

    public int Length => _queue.Length;
    public Bitboard Body => _bitboard;

    public int Hp => _life.Hp;
    public bool IsDead => _life.IsDead;

    public bool IsGrowthPending => _life.IsGrowthPending;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UpdateAfterMove(ushort newHead, bool ateFood, int damage)
    {
        if (IsDead) return;

        var wasGrowing = _life.ConsumePendingGrowth();

        if (!wasGrowing && !ateFood)
        {
            var oldTailPos = _queue.Dequeue();
            if (oldTailPos != _queue.PeekTail) _bitboard.Unset(oldTailPos);
        }

        _queue.Enqueue(newHead);
        _bitboard.Set(newHead);

        if (ateFood)
        {
            _life.FullCure();
            _life.ScheduleGrowth();
        }
        else
            _life.Damage((byte)damage);
    }

    public void Kill() => _life.Kill();
    public bool IsOnBody(ushort position) => _bitboard.IsSet(position);
}