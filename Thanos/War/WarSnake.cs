// Assumendo che Bitboard sia qui

namespace Thanos.War;

public ref struct WarSnake(SnakeHealth snakeHealth, SnakeAnatomy snakeAnatomy, Bitboard bitboard)
{
    private SnakeHealth _snakeHealth = snakeHealth;
    private SnakeAnatomy _snakeAnatomy = snakeAnatomy;

    private readonly Bitboard _bitboard = bitboard;

    public int HP => _snakeHealth.Points;
    public bool IsDead => _snakeHealth.IsDead;
    public ushort Length => _snakeAnatomy.Length;

    public void Move(ushort newHeadPos, ushort oldTailPos, bool ateFood, byte damage)
    {
        if (_snakeHealth.IsDead) return;

        // 1. Delega ad Anatomy il compito di processare la crescita
        _snakeAnatomy.ProcessPendingGrowth();

        // 2. Logica di movimento standard
        _snakeHealth.Damage(damage);
        if (_snakeHealth.IsDead) return;

        // 3. Se mangia, delega ad Anatomy il compito di schedulare la crescita futura
        if (ateFood)
        {
            _snakeHealth.FullCure();
            _snakeAnatomy.ScheduleGrowth();
        }
        
        // 4. Aggiorna la posizione
        _bitboard.Set(newHeadPos);
        if (!ateFood)
        {
            _bitboard.Unset(oldTailPos);
        }
    }

    public void TakeDamage(byte amount) => _snakeHealth.Damage(amount);

    public void Kill() => _snakeHealth.Kill();

    public bool IsOnBody(ushort position) => _bitboard.IsSet(position);
}