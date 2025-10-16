using Thanos.Extensions;

namespace Thanos.War;

public ref struct WarSnake
{
    private ref WarSnakeHeader _header;
    private readonly Bitboard _bitboard;

    public WarSnake(ref WarSnakeHeader header, Span<byte> memory)
    {
        _header = ref header;
        _bitboard = new Bitboard(memory);
    }

    public void Initialize(byte points, ReadOnlySpan<ushort> body)
    {
        var length = (ushort)body.Length;
        var head = body[0];
        var tail = body[^1];
        
        _header.PlacementNew(length, head, tail, points);

        _bitboard.Clear();
        foreach (var segment in body) _bitboard.Set(segment);
    }

    // --- PROPRIETÀ DI STATO ---
    public ushort Head => _header.Head;
    public ushort Tail => _header.Tail;
    public ushort Length => _header.Length;
    public Bitboard Body => _bitboard;
    public int HP => _header.Points;
    public bool IsDead => _header.IsDead;
    public bool WillGrow => _header.IsGrowthPending;
    
    public void UpdateAfterMove(ushort newHead, ushort newTail, bool ateFood, int damage)
    {
        if (IsDead) return;

        var oldTail = _header.Tail;

        _header.Head = newHead;
        _header.Damage((byte)damage);

        #if DEBUG
        Console.WriteLine("============================================================================================================================");
        Console.WriteLine("==== SNAKE SNAKE SNAKE SNAKE SNAKE SNAKE SNAKE SNAKE SNAKE SNAKE SNAKE SNAKE SNAKE SNAKE SNAKE SNAKE SNAKE SNAKE SNAKE  ====");
        Console.WriteLine("============================================================================================================================");
        
        Console.WriteLine("--- Stato Serpente PRIMA ---" );
        Console.WriteLine(_bitboard.ToGridString(11, 11));
        #endif
        
        _bitboard.Set(newHead);
        
        #if DEBUG
        Console.WriteLine("--- Stato Serpente DOPO ---" );
        Console.WriteLine(_bitboard.ToGridString(11, 11));
        #endif

        if (ateFood)
        {
            _header.FullCure();
            _header.ScheduleGrowth();
        }
        
        if (_bitboard.PopCount() > _header.Length)
        {
            _bitboard.Unset(oldTail);
            _header.Tail = newTail;
        }
        
        #if DEBUG
        Console.WriteLine("--- Stato Serpente FINE ---" );
        Console.WriteLine(_bitboard.ToGridString(11, 11));
        
        Console.WriteLine("===========================================================================================================================");
        Console.WriteLine("==== SNAKE SNAKE SNAKE SNAKE SNAKE SNAKE SNAKE SNAKE SNAKE SNAKE SNAKE SNAKE SNAKE SNAKE SNAKE SNAKE SNAKE SNAKE SNAKE ====");
        Console.WriteLine("===========================================================================================================================");
        #endif
    }

    public void Kill() => _header.Kill();
    public bool IsOnBody(ushort position) => _bitboard.IsSet(position);
}