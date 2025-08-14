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
    
    public uint Width, Height, Area, _bitboardSegments;

    private ulong* _foodBitboard;
    private ulong* _hazardBitboard;
    private ulong* _snakesBitboard;

    public Bitboard Food => new(_foodBitboard, _bitboardSegments);
    public Bitboard Hazard => new(_hazardBitboard, _bitboardSegments);
    public Bitboard Snakes => new(_snakesBitboard, _bitboardSegments);

    public static void PlacementNew(WarField* fieldPtr, in WarContext context, ReadOnlySpan<Coordinate> food, ReadOnlySpan<Coordinate> hazards, uint bitboardSegments, ulong* foodBitboardPtr, ulong* hazardBitboardPtr, ulong* snakesBitboardPtr)
    {
        // 1. Inizializza i campi e salva i puntatori
        fieldPtr->Width = context.Width;
        fieldPtr->Height = context.Height;
        fieldPtr->Area = context.Area;
        fieldPtr->_bitboardSegments = bitboardSegments;
        fieldPtr->_foodBitboard = foodBitboardPtr;
        fieldPtr->_hazardBitboard = hazardBitboardPtr;
        fieldPtr->_snakesBitboard = snakesBitboardPtr;

        // 2. Pulisce e popola i bitboard usando la nuova API pulita!
        fieldPtr->Food.ClearAll();
        fieldPtr->Hazard.ClearAll();
        fieldPtr->Snakes.ClearAll();

        // 3. Popola i bitboard usando l'API delle proprietà
        foreach (ref readonly var coordinate in food) fieldPtr->Food.Set(To1D(in coordinate, fieldPtr->Width));
        foreach (ref readonly var coordinate in hazards) fieldPtr->Hazard.Set(To1D(in coordinate, fieldPtr->Width));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void SetSnakeBit(ushort position1D)
    {
        SetBit(new Span<ulong>(_snakesBitboard, (int)_bitboardSegments), position1D);
    }
    
    /// <summary>
    /// Aggiorna i bitboard per riflettere il movimento di un serpente.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UpdateSnakePosition(ushort oldTail, ushort newHead, bool hasEaten)
    {
        var snakesSpan = new Span<ulong>(_snakesBitboard, (int)_bitboardSegments);
        SetBit(snakesSpan, newHead);

        if (!hasEaten)
        {
            ClearBit(snakesSpan, oldTail);
        }
        else
        {
            // Se il cibo è stato mangiato, lo rimuove dal bitboard del cibo.
            ClearBit(new Span<ulong>(_foodBitboard, (int)_bitboardSegments), newHead);
        }
    }

    /// <summary>
    /// Rimuove completamente un serpente dal bitboard dei serpenti, usando uno Span per sicurezza.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RemoveSnake(ReadOnlySpan<ushort> body)
    {
        var snakesSpan = new Span<ulong>(_snakesBitboard, (int)_bitboardSegments);
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
}