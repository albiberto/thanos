using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Thanos.Enums;
using Thanos.SourceGen;

namespace Thanos.War;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WarSnake
{
    public const int HeaderSize = Constants.CacheLineSize;
    private const int PaddingSize = Constants.CacheLineSize - sizeof(int) * 5 - sizeof(ushort) * 1;

    // === CACHE LINE 1: Header ===
    private uint _capacity;
    private uint _nextHeadIndex;

    public int Health;
    public uint Length;
    public ushort Head;
    public uint TailIndex;

    private fixed byte _padding[PaddingSize];

    // === CACHE LINE 2+: Body ===
    public fixed ushort Body[1];

    /// <summary>
    /// Inizializza l'intero stato di questo WarSnake in-place.
    /// Questo metodo agisce come un "costruttore" per una struct in memoria non gestita.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Initialize(in Snake snakeDto, in WarField field, uint capacity)
    {
        var length = (uint)Math.Min(snakeDto.Length, capacity);
        var sourceBody = snakeDto.Body.AsSpan();

        // 1. Inizializza i campi di stato dell'header
        Health = snakeDto.Health;
        Length = length;
        _capacity = capacity;
        TailIndex = 0;
        _nextHeadIndex = length & (capacity - 1);

        // 2. Esegue un unico ciclo per convertire le coordinate e popolarle
        for (var i = 0; i < length; i++)
        {
            // Converte la coordinata 2D in 1D
            var index = (int)(length - 1 - i); // Inverte l'ordine per Coda -> Testa
            var coord1D = field.To1D(sourceBody[index]);
            
            // a) Scrive nel proprio buffer del corpo (in ordine Coda -> Testa)
            Body[i] = coord1D;

            // b) Notifica a WarField di "disegnarsi" sulla mappa
            field.SetSnakeBit(coord1D);
        }
    
        // 3. Imposta la sua testa
        Head = length > 0 
            ? Body[length - 1] 
            : ushort.MaxValue; // Valore sentinella per serpenti di lunghezza 0
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Move(ushort newHeadPosition, bool hasEaten, int damage = 0)
    {
        ref var health = ref Health;
        ref var length = ref Length;
        ref var tailIndex = ref TailIndex;
        ref var nextHeadIndex = ref _nextHeadIndex;
        ref var capacity = ref _capacity;

        if (hasEaten) health = 100;
        else health -= damage + 1;

        if (health <= 0) return;

        var capacityMask = capacity - 1;

        Body[nextHeadIndex] = Head;
        Head = newHeadPosition;
        nextHeadIndex = (nextHeadIndex + 1) & capacityMask;

        if (hasEaten && length < capacity)
            length++;
        else
            tailIndex = (tailIndex + 1) & capacityMask;
    }

    public readonly bool Dead => Health <= 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Kill() => Health = 0;
}