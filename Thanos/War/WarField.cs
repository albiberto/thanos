using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Thanos.Enums;
using Thanos.SourceGen;

namespace Thanos.War;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WarField
{
    public const int TotalBitboards = 3;

    // Campi pubblici per un facile accesso
    public uint Width;
    public uint Height;
    public uint Area;
    private uint _bitboardSegmentCount; // Aggiunto per creare Span della dimensione corretta

    // I puntatori rimangono, perché la struct non può contenere Span.
    // Sono l'implementazione interna, non l'API pubblica.
    private ulong* _foodBitboard;
    private ulong* _hazardBitboard;
    private ulong* _snakesBitboard;

    /// <summary>
    /// Metodo "costruttore" che inizializza la struct partendo da aree di memoria sicure (Span).
    /// Questa è la nuova API pubblica per l'inizializzazione.
    /// </summary>
    public static void PlacementNew(
        Span<byte> fieldSpan,
        Span<ulong> foodBitboardSpan,
        Span<ulong> hazardBitboardSpan,
        Span<ulong> snakesBitboardSpan,
        in WarContext context,
        in Board board)
    {
        // Ottiene un riferimento alla nostra istanza di WarField.
        ref var field = ref MemoryMarshal.GetReference(MemoryMarshal.Cast<byte, WarField>(fieldSpan));

        // Inizializza le proprietà.
        field.Width = context.Width;
        field.Height = context.Height;
        field.Area = context.Area;
        // Salva il numero di segmenti, ci servirà per creare gli Span dopo.
        field._bitboardSegmentCount = (uint)foodBitboardSpan.Length;

        // Assegna i puntatori interni in modo sicuro, partendo dagli Span.
        // Questa è l'unica parte "unsafe" e rimane incapsulata qui.
        field._foodBitboard = (ulong*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(foodBitboardSpan));
        field._hazardBitboard = (ulong*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(hazardBitboardSpan));
        field._snakesBitboard = (ulong*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(snakesBitboardSpan));

        // Popola i bitboard usando gli span, che è più sicuro e leggibile.
        foreach (ref readonly var foodCoord in board.Food.AsSpan())
        {
            SetBit(foodBitboardSpan, To1D(in foodCoord, field.Width));
        }
        foreach (ref readonly var hazardCoord in board.Hazards.AsSpan())
        {
            SetBit(hazardBitboardSpan, To1D(in hazardCoord, field.Width));
        }
    }

    // --- METODI DI SCRITTURA (usano Span per sicurezza) ---

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void SetSnakeBit(ushort position1D)
    {
        SetBit(new Span<ulong>(_snakesBitboard, (int)_bitboardSegmentCount), position1D);
    }
    
    /// <summary>
    /// Aggiorna i bitboard per riflettere il movimento di un serpente.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UpdateSnakePosition(ushort oldTail, ushort newHead, bool hasEaten)
    {
        var snakesSpan = new Span<ulong>(_snakesBitboard, (int)_bitboardSegmentCount);
        SetBit(snakesSpan, newHead);

        if (!hasEaten)
        {
            ClearBit(snakesSpan, oldTail);
        }
        else
        {
            // Se il cibo è stato mangiato, lo rimuove dal bitboard del cibo.
            ClearBit(new Span<ulong>(_foodBitboard, (int)_bitboardSegmentCount), newHead);
        }
    }

    /// <summary>
    /// Rimuove completamente un serpente dal bitboard dei serpenti, usando uno Span per sicurezza.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RemoveSnake(ReadOnlySpan<ushort> body)
    {
        var snakesSpan = new Span<ulong>(_snakesBitboard, (int)_bitboardSegmentCount);
        foreach (var position in body) ClearBit(snakesSpan, position);
    }

    // --- METODI DI LETTURA "HOT PATH" (usano puntatori per massime performance) ---

    /// <summary>
    /// Controlla se una casella è occupata. Operazione O(1) ultra-veloce.
    /// Manteniamo i puntatori qui perché è un "hot path" di sola lettura
    /// e il JIT può ottimizzare l'accesso diretto in modo eccezionale.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool IsOccupied(ushort position1D)
    {
        if (position1D == ushort.MaxValue) return true; // Collisione con un muro
        var ulongIndex = position1D >> 6;
        var bitMask = 1UL << (position1D & 63);
        return ((_hazardBitboard[ulongIndex] | _snakesBitboard[ulongIndex]) & bitMask) != 0;
    }

    /// <summary>
    /// Controlla se in una data casella è presente del cibo. Operazione O(1).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool IsFood(ushort position1D)
    {
        if (position1D >= Area) return false;
        var index = position1D >> 6;
        var mask = 1UL << (position1D & 63);
        return (_foodBitboard[index] & mask) != 0;
    }
    
    // --- METODI HELPER ---

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ushort To1D(in Coordinate coord) => (ushort)(coord.Y * Width + coord.X);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ushort To1D(in Coordinate coord, uint width) => (ushort)(coord.Y * width + coord.X);

    // Helper privati che ora operano su Span<ulong> per coerenza e sicurezza.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void SetBit(Span<ulong> board, ushort position1D) => board[position1D >> 6] |= 1UL << (position1D & 63);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ClearBit(Span<ulong> board, ushort position1D) => board[position1D >> 6] &= ~(1UL << (position1D & 63));
}