using System.Runtime.CompilerServices;
using Thanos.SourceGen;
using Thanos.War.Structures;

namespace Thanos.War;

public ref struct WarSnake(ref WarSnakeLife life, Bitboard bitboard, CircularQueue queue)
{
    private ref WarSnakeLife _life = ref life;

    private readonly Bitboard _bitboard = bitboard;
    public CircularQueue _queue = queue;

    public void Initialize(in Snake snakeData)
    {
        _bitboard.Clear();
        _queue.Clear();
        _life.SetHP(snakeData.Health);

        for (var i = snakeData.Body.Length - 1; i >= 0; i--)
        {
            var part = snakeData.Body[i];
            _queue.Enqueue(part);
            _bitboard.Set(part);
        }
    }

    // --- PROPRIETÀ E ACCESSORI ---
    public ushort Head => _queue.PeekHead;
    public ushort Tail => _queue.PeekTail;
    public ushort ElementBeforeTail => _queue.PeekElementBeforeTail;
    public int Length => _queue.Length;
    public Bitboard Body => _bitboard;
    public int HP => _life.HP;
    public bool IsDead => _life.IsDead;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ushort RemoveTail() => _queue.Dequeue();

    public void UpdateAfterMove(ushort newHead, bool ateFood, int damage)
    {
        if (IsDead) return;
        
        var wasGrowing = _life.ConsumePendingGrowth();

        if (!wasGrowing && !ateFood)
        {
            var oldTailPos = RemoveTail();
            _bitboard.Unset(oldTailPos);
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