using System.Numerics;
using System.Runtime.InteropServices;
using Thanos.War.Grid.Memory;
using Thanos.War.Snake;

namespace Thanos.War.Grid;

[StructLayout(LayoutKind.Sequential)]
public readonly ref struct WarGrid
{
    public readonly ref Geography Geography;

    public readonly Bitboard Food;
    public readonly Bitboard Hazards;
    public readonly Bitboard Snakes;

    private readonly ReadOnlySpan<ushort> _neighborsBoard;

    public WarGrid(WarGridMemoryView view)
    {
        Geography = ref view.Geography;

        Food = view.Food;
        Hazards = view.Hazards;
        Snakes = view.Snakes;

        _neighborsBoard = view.NeighborsBoard;
    }

    public bool IsSet(ushort position) => position == ushort.MaxValue || Snakes.IsSet(position);
    public bool IsUnset(ushort position) => position != ushort.MaxValue && Snakes.IsUnset(position);

    public bool IsFood(ushort position) => Food.IsSet(position);

    public bool IsHazard(ushort position) => Hazards.IsSet(position);

    public ushort GetNeighbor(ushort position, byte move) => _neighborsBoard[position * 4 + BitOperations.TrailingZeroCount(move)];

    // Dentro WarGrid.cs
    public void SynchronizeSnakeOnGrid(WarSnake snake, ushort oldTail, bool hasEaten)
    {
        // L'approccio più semplice e sicuro: cancella la coda e ridisegna tutto il corpo.
        // Questo gestisce correttamente tutti i casi, inclusi i segmenti sovrapposti.
        if (!hasEaten) Snakes.Unset(oldTail);

        // Ridisegna l'intero corpo del serpente sulla bitboard per garantire la coerenza.
        snake.GetSpans(out var bodyFirst, out var bodySecond);
        foreach (var pos in bodyFirst) Snakes.Set(pos);
        foreach (var pos in bodySecond) Snakes.Set(pos);
    }

    public void RemoveFood(ushort position) => Food.Unset(position);

    public void RemoveSnake(WarSnake snake)
    {
        snake.GetSpans(out var bodyFirst, out var bodySecond);
        foreach (var pos in bodyFirst) Snakes.Unset(pos);
        foreach (var pos in bodySecond) Snakes.Unset(pos);
    }
}