using Thanos.War.Grid;
using Thanos.War.Snake; // Assumendo che Bitboard sia qui

public readonly ref struct WarSnake(Health health, Anatomy anatomy, Bitboard bitboard)
{
    private readonly Health _health = health;
    private readonly Anatomy _anatomy = anatomy;
    
    // Il campo ora è del tuo tipo Bitboard.
    private readonly Bitboard _bitboard = bitboard;

    // L'API Pubblica del Serpente...
    public int HP => _health.Points;
    
    public void Move(ushort newHeadPos, ushort oldTailPos, bool ateFood)
    {
        // ... Logica di Health e Anatomy ...
        if (_health.IsDead) return;
        // ...

        // Ora usi la TUA Bitboard, che è pulita e incapsulata.
        if (!ateFood)
        {
            _bitboard.Unset(oldTailPos);
        }
        _bitboard.Set(newHeadPos);
    }

    public void Kill() => _health.Kill();
}