using System.Runtime.CompilerServices;
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
        _life.SetHp(snakeData.Health);
        _life.ResetStack();

        var body = snakeData.Body;
        var length = body.Length;

        for (var i = length - 1; i > 0; i--)
        {
            if (body[i] == body[i - 1])
            {
                _life.ScheduleGrowth();
            }
            else 
            {
                break; 
            }
        }

        for (var i = length - 1; i >= 0; i--)
        {
            var part = body[i];
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

        var isGrowing = _life.ConsumePendingGrowth();

        if (!isGrowing)
        {
            var oldTailPos = _queue.Dequeue();

            var newTailPos = _queue.PeekTail;
            if (oldTailPos != newTailPos) _bitboard.Unset(oldTailPos);
        }

        _queue.Enqueue(newHead);
        _bitboard.Set(newHead);

        if (ateFood)
        {
            _life.FullCure();
            _life.ScheduleGrowth();
        }
        else
        {
            _life.Damage((byte)damage);
        }
    }

    public void Kill() => _life.Kill();
    public bool IsOnBody(ushort position) => _bitboard.IsSet(position);
}