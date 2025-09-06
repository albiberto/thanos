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
        var tail = body[^1]; // ^1 significa "ultimo elemento"
    
        // Aggiorna lo stato persistente nell'header
        _header.PlacementNew(length, head, tail, points);
    
        // Disegna il corpo del serpente nel suo bitboard individuale
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

    // --- METODI DI COMANDO ---

    /// <summary>
    /// Metodo principale che esegue i comandi dell'Arena per aggiornare lo stato
    /// del serpente dopo una mossa.
    /// </summary>
    public void UpdateAfterMove(ushort newHead, ushort newTail, bool ateFood, int damage)
    {
        if (IsDead) return;

        // Salva la vecchia posizione della coda PRIMA di modificarla.
        // Ci servirà per pulire il bitboard.
        var oldTail = _header.Tail;

        // 1. Aggiorna lo stato nell'Header (la "mente" del serpente)
        _header.Head = newHead;
        _header.ProcessPendingGrowth();
        _header.Damage((byte)damage);

        if (ateFood)
        {
            _header.FullCure();
            _header.ScheduleGrowth();
        }
        else // La coda si sposta solo se non abbiamo mangiato
        {
            _header.Tail = newTail;
        }

        // 2. Sincronizza il Bitboard (la "pelle" del serpente)
        _bitboard.Set(newHead);
        if (!ateFood)
        {
            _bitboard.Unset(oldTail);
        }
    }

    public void Kill() => _header.Kill();
    public bool IsOnBody(ushort position) => _bitboard.IsSet(position);
}