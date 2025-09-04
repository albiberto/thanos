using System.Runtime.InteropServices;
using Thanos.War.Memory.Views;
using Thanos.War.Snake;

namespace Thanos.War.Grid;

[StructLayout(LayoutKind.Sequential)]
public readonly ref struct WarGrid(WarGridMemoryView view)
{
    public const int BitboardCount = 3; // Food, Hazards, Snakes

    public readonly Bitboard Food = view.Food;
    public readonly Bitboard Hazards = view.Hazards;
    public readonly Bitboard Snakes = view.Snakes;

    public bool IsSet(ushort position) => position == ushort.MaxValue || Snakes.IsSet(position);
    public bool IsUnset(ushort position) => position != ushort.MaxValue && Snakes.IsUnset(position);

    public bool IsFood(ushort position) => Food.IsSet(position);

    public bool IsHazard(ushort position) => Hazards.IsSet(position);

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