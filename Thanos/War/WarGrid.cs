using System.Runtime.CompilerServices;
using Thanos.Common; // Per la struct Moves

namespace Thanos.War;

/// <summary>
/// Rappresenta il mondo di gioco. Contiene i bitboard di stato
/// e offre metodi per interrogare e modificare la mappa.
/// </summary>
public readonly ref struct Grid(int area, Span<byte> food, Span<byte> hazards, Span<byte> allSnakes, ReadOnlySpan<ushort> neighbors)
{
    public  int Area { get; } = area;
    
    private readonly Bitboard _food = new(food);
    private readonly Bitboard _hazards = new(hazards);
    private readonly Bitboard _allSnakesBitboard = new(allSnakes);
    private readonly ReadOnlySpan<ushort> _neighbors = neighbors;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort GetNeighbor(ushort position, byte move) => _neighbors[position * 4 + move.NumberOfTrailingZeros()];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsOccupied(ushort position) => _allSnakesBitboard.IsSet(position);
    
    // Metodo statico perché non dipende dallo stato interno della Grid (è una regola universale)
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsValid(ushort position) => position != ushort.MaxValue;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsFood(ushort position) => _food.IsSet(position);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsHazard(ushort position) => _hazards.IsSet(position);

    public void RemoveFood(ushort position) => _food.Unset(position);
    
    public void RemoveSnakeBody(WarSnake snake) => _allSnakesBitboard.Xor(snake.Body);

    /// <summary>
    /// Aggiorna il bitboard combinato in modo incrementale.
    /// </summary>
    public void UpdateSnakePosition(ushort oldTail, ushort newHead, bool ateFood)
    {
        _allSnakesBitboard.Set(newHead);
        if (!ateFood) _allSnakesBitboard.Unset(oldTail);
    }
}