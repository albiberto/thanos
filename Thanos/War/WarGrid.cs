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
    
    public readonly Bitboard Food = new(food);
    public readonly Bitboard Hazards = new(hazards);
    public readonly Bitboard Snakes = new(allSnakes);
    private readonly ReadOnlySpan<ushort> _neighbors = neighbors;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort GetNeighbor(ushort position, byte move) => _neighbors[position * 4 + move.NumberOfTrailingZeros()];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsOccupied(ushort position) => Snakes.IsSet(position);
    
    // Metodo statico perché non dipende dallo stato interno della Grid (è una regola universale)
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsValid(ushort position) => position != ushort.MaxValue;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsFood(ushort position) => Food.IsSet(position);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsHazard(ushort position) => Hazards.IsSet(position);

    public void RemoveFood(ushort position) => Food.Unset(position);
    
    public void RemoveSnakeBody(WarSnake snake) => Snakes.Xor(snake.Body);

    /// <summary>
    /// Aggiorna il bitboard combinato in modo incrementale.
    /// </summary>
    public void UpdateSnakePosition(ushort oldTail, ushort newHead, bool ateFood)
    {
        Snakes.Set(newHead);
        if (!ateFood) Snakes.Unset(oldTail);
    }
}