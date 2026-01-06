using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using Thanos.Common;

namespace Thanos.Shared;

public readonly ref struct NeighborsMatrix(ReadOnlySpan<ushort> buffer)
{
    private readonly ReadOnlySpan<ushort> _buffer = buffer;

    /// <summary>
    /// Restituisce il vicino nella direzione specificata dalla maschera di bit.
    /// Ottimizzato per l'accesso scalare usando istruzioni hardware per calcolare l'offset.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort Get(ushort currentPos, byte moveMask) 
        => _buffer[currentPos * 4 + BitOperations.TrailingZeroCount(moveMask)];

    /// <summary>
    /// Restituisce i 4 vicini packed in un registro SIMD 64-bit (4 x ushort).
    /// Layout: [0]=Up, [1]=Down, [2]=Left, [3]=Right.
    /// Fondamentale per i check vettoriali (Suicidio, DeadEnd) in Arena.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector64<ushort> GetAll(ushort currentPos) 
    {
        // Calcoliamo l'indirizzo di memoria base per la cella corrente.
        // currentPos * 4 perché ogni cella ha 4 vicini contigui in memoria.
        ref var address = ref Unsafe.Add(ref MemoryMarshal.GetReference(_buffer), (nint)currentPos * 4);
        
        // Carichiamo 64 bit (8 byte) in un colpo solo direttamente nei registri SIMD.
        return Vector64.LoadUnsafe(ref address);
    }

    /// <summary>
    /// Verifica se la posizione è valida (dentro la griglia).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsValid(ushort position) => position != ushort.MaxValue;

    /// <summary>
    /// Verifica se la posizione è fuori dai bordi (Muro).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsOutOfBound(ushort position) => position == ushort.MaxValue;
}