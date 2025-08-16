using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Thanos.SourceGen;

namespace Thanos.War;

[StructLayout(LayoutKind.Sequential)]
public struct WarSnakeHeader
{
    public int Health;
    public uint Capacity;
    public uint Length;
    public ushort Head;
    public uint NextHeadIndex;
    public uint TailIndex;
}

public ref struct WarSnake
{
    // Riferimenti ai dati sottostanti
    private ref WarSnakeHeader _header;
    private readonly Span<ushort> _body;

    /// <summary>
    /// COSTRUTTORE 2 (per la "vista"):
    /// Si collega semplicemente alla memoria già inizializzata.
    /// </summary>
    public WarSnake(ref WarSnakeHeader header, Span<ushort> body)
    {
        _header = ref header;
        _body = body;
    }
    
    /// <summary>
    /// COSTRUTTORE "TUTTOFARE":
    /// 1. Si collega alla memoria grezza (header e body).
    /// 2. Inizializza quella memoria usando i dati forniti (initialSnakeData).
    /// </summary>
    public WarSnake(ref WarSnakeHeader header, Span<ushort> body, in Snake snake, ref WarField field)
    {
        // Fase 1: Collegamento alla memoria
        _header = ref header;
        _body = body;

        // Fase 2: Inizializzazione della memoria (la logica del vecchio metodo statico è ora qui)
        var capacity = (uint)_body.Length;
        var length = (uint)System.Math.Min(snake.Length, capacity);

        _header.Capacity = capacity;
        _header.Length = length;
        _header.Health = snake.Health;
        _header.TailIndex = 0;
        _header.NextHeadIndex = length & (capacity - 1); // Ottimo per capacità che sono potenze di 2

        // Popola il corpo e aggiorna la bitboard in WarField
        for (var j = 0; j < length; j++)
        {
            var index = (int)(length - 1 - j);
            ref readonly var coordinate = ref snake.Body[index];
            var coord1D = field.To1D(in coordinate);

            _body[j] = coord1D;
            field.SetSnakeBit(coord1D);
        }

        _header.Head = length > 0
            ? _body[(int)length - 1]
            : ushort.MaxValue;
    }

    // --- PROPRIETÀ ---
    // Health ora ha un setter pubblico, che è corretto. Rimosso 'readonly'.
    public int Health { readonly get => _header.Health; private set => _header.Health = value; }
    public readonly ushort Head => _header.Head;
    public readonly ushort Tail => _body[(int)_header.TailIndex];
    public readonly uint Length => _header.Length;
    public readonly bool Dead => Health <= 0;
    
    // --- METODI ---
    public void Kill() => Health = 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Move(ushort newHeadPosition, bool hasEaten, int damage = 1)
    {
        Health = hasEaten ? 100 : Health - damage;
        if (Dead) return;
        
        PushHead(newHeadPosition);
        
        if (hasEaten) 
            IncrementLength(); 
        else 
            PopTail();
    }
    
    public readonly void GetSpans(out Span<ushort> first, out Span<ushort> second)
    {
        var tailIndex = (int)_header.TailIndex;
        var length = (int)_header.Length;
        var capacity = (int)_header.Capacity;

        if (length == 0)
        {
            first = Span<ushort>.Empty;
            second = Span<ushort>.Empty;
            return;
        }

        var headIndex = (tailIndex + length - 1) & (capacity - 1);

        if (tailIndex <= headIndex)
        {
            first = _body.Slice(tailIndex, length);
            second = Span<ushort>.Empty;
        }
        else
        {
            var firstLength = capacity - tailIndex;
            first = _body.Slice(tailIndex, firstLength);
            second = _body[..(length - firstLength)];
        }
    }
    
    private void PushHead(ushort newHeadPosition)
    {
        _body[(int)_header.NextHeadIndex] = _header.Head;
        _header.Head = newHeadPosition;
        _header.NextHeadIndex = (_header.NextHeadIndex + 1) & (_header.Capacity - 1);
    }

    private void PopTail() => _header.TailIndex = (_header.TailIndex + 1) & (_header.Capacity - 1);

    private void IncrementLength()
    {
        if (_header.Length < _header.Capacity) _header.Length++;
    }
}