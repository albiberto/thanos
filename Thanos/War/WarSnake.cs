using System.Runtime.CompilerServices;
using Thanos.SourceGen;

namespace Thanos.War;

public readonly ref struct WarSnake
{
    private readonly ref WarSnakeHeader _header;
    private readonly Bitboard _bodyBitboard;
    private readonly Span<ushort> _bodyParts;
    private readonly int _capacity;

    public WarSnake(ref WarSnakeHeader header, Span<byte> bodyBitboardMemory, Span<ushort> bodyParts, int capacity)
    {
        _header = ref header;
        _bodyBitboard = new Bitboard(bodyBitboardMemory);
        _bodyParts = bodyParts;
        _capacity = capacity;
    }

    public void Initialize(in Snake snakeData)
    {
        _header.HP = snakeData.Health;
        _bodyBitboard.Clear();
        _header.HeadIndex = 0;
        _header.TailIndex = 0;
        
        // Inizializza il buffer e la bitboard partendo dalla coda
        for (int i = snakeData.Body.Length - 1; i >= 0; i--)
        {
            var part = snakeData.Body[i];
            AddHead(part);
            _bodyBitboard.Set(part);
        }
    }

    // --- PROPRIETÀ DI STATO ---
    public ushort Head => _header.Head;
    public ushort Tail => _bodyParts[_header.TailIndex]; // La coda è il primo elemento nel buffer
    public int Length => (_header.HeadIndex - _header.TailIndex + _capacity) % _capacity;
    public Bitboard Body => _bodyBitboard;
    public int HP => _header.HP;
    public bool IsDead => _header.HP <= 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AddHead(ushort newHeadPos)
    {
        _bodyParts[_header.HeadIndex] = newHeadPos;
        _header.HeadIndex = (_header.HeadIndex + 1) % _capacity;
        _header.Head = newHeadPos;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ushort RemoveTail()
    {
        ushort oldTailPos = _bodyParts[_header.TailIndex];
        _header.TailIndex = (_header.TailIndex + 1) % _capacity;
        return oldTailPos;
    }

    public void UpdateAfterMove(ushort newHead, bool ateFood, int damage)
    {
        if (IsDead) return;

        // Aggiungi la nuova testa
        AddHead(newHead);
        _bodyBitboard.Set(newHead);

        // Gestione della coda e della crescita
        if (ateFood)
        {
            _header.FullCure();
            // Non rimuoviamo la coda, facendo crescere il serpente di 1.
        }
        else
        {
            // Rimuovi la vecchia coda dal buffer e dalla bitboard
            ushort oldTailPos = RemoveTail();
            
            // Controlla se la posizione della vecchia coda è ancora occupata
            // da un'altra parte del corpo prima di cancellare il bit.
            bool isPosStillOccupied = false;
            for (int i = _header.TailIndex; i != _header.HeadIndex; i = (i + 1) % _capacity)
            {
                if (_bodyParts[i] == oldTailPos)
                {
                    isPosStillOccupied = true;
                    break;
                }
            }
            
            if (!isPosStillOccupied)
            {
                _bodyBitboard.Unset(oldTailPos);
            }
        }
        
        _header.Damage((byte)damage);
    }

    public void Kill() => _header.Kill();
    public bool IsOnBody(ushort position) => _bodyBitboard.IsSet(position);
}