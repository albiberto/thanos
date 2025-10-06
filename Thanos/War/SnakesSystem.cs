using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Thanos.Memory;

namespace Thanos.War;

public readonly ref struct SnakesSystem
{
    private readonly Span<byte> _memory;
    private readonly ref readonly MemoryLayout _layout;

    public SnakesSystem(Span<byte> memory, in MemoryLayout layout, int count)
    {
        _memory = memory;
        _layout = ref layout;

        Count = count;
    }

    public int Count { get; }
    public Span<byte> Raw => _memory;
    public WarSnake Me => this[0];
    public WarSnake this[int index] => Build(index);

    /// <summary>
    /// NUOVO: Proprietà per leggere e scrivere l'indice del giocatore di turno.
    /// Manipola direttamente i primi 4 byte della memoria grezza.
    /// </summary>
    public int PlayerToMoveIndex
    {
        get
        {
            // Prende un riferimento all'inizio della memoria (offset 0).
            ref var memoryLocationRef = ref MemoryMarshal.GetReference(_memory);
            // Interpreta i byte a partire da quella locazione come un intero e lo restituisce.
            return Unsafe.As<byte, int>(ref memoryLocationRef);
        }
        set
        {
            // Prende un riferimento all'inizio della memoria (offset 0).
            ref var memoryLocationRef = ref MemoryMarshal.GetReference(_memory);
            // Scrive il valore intero in quella locazione di memoria.
            Unsafe.As<byte, int>(ref memoryLocationRef) = value;
        }
    }

    private WarSnake Build(int index)
    {
        // 1. Trova l'Header del serpente.
        var headerOffset = _layout.GetSnakeHeaderOffset(index);
        var headerMemory = _memory.Slice(headerOffset, _layout.HeaderStride);

        // 2. Ora otteniamo un singolo riferimento all'header completo.
        ref var headerBaseRef = ref MemoryMarshal.GetReference(headerMemory);
        ref var header = ref Unsafe.As<byte, WarSnakeHeader>(ref headerBaseRef);

        // 3. Trova il Bitboard.
        var bitboardOffset = _layout.GetSnakeBitboardOffset(index);
        var bitboardByteSpan = _memory.Slice(bitboardOffset, _layout.BitboardSize);

        // 4. Il costruttore di WarSnake
        return new WarSnake(ref header, bitboardByteSpan);
    }
}