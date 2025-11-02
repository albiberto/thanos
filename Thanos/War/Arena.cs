using System.Runtime.CompilerServices;
using Thanos.Common;
using Thanos.SourceGen;
using Thanos.PreWarm;
using Thanos.War.Structures; // Aggiunto per StringBuilder

namespace Thanos.War;

/// <summary>
///     Rappresenta una singola istanza di gioco. È il cervello che orchestra la logica.
/// </summary>
public readonly ref struct Arena(
    SnakesSystem system,
    Bitboard food,
    Bitboard hazards,
    Bitboard snakes,
    Dictionary<string, int> map,
    NeighborsGrid neighborsGrid,
    ReadOnlySpan<Coordinate> conversionsMap)
{
    public readonly SnakesSystem System = system;

    public readonly Bitboard Food = food;
    public readonly Bitboard Hazards = hazards;
    public readonly Bitboard Snakes = snakes;

    private readonly NeighborsGrid _neighborsGrid = neighborsGrid;
    private readonly ReadOnlySpan<Coordinate> _conversionsMap = conversionsMap;

    public void InitializeFromRequest(in Request request)
    {
        Food.Clear();
        Hazards.Clear();
        Snakes.Clear();

        for (var i = 0; i < System.Count; i++) System[i].Kill();

        var board = request.Board;

        foreach (var snakeData in board.Snakes)
            if (map.TryGetValue(snakeData.Id, out var snakeIndex))
            {
                var snake = System[snakeIndex];
                snake.Initialize(snakeData);
                Snakes.Or(snake.Body);
            }

        foreach (var foodPosition in board.Food) Food.Set(foodPosition);
        foreach (var hazardPosition in board.Hazards) Hazards.Set(hazardPosition);
    }

    public void CloneFrom(in Arena source)
    {
        source.System.Raw.CopyTo(System.Raw);

        source.Food.CopyTo(Food);
        source.Hazards.CopyTo(Hazards);
        source.Snakes.CopyTo(Snakes);
    }

    // File: Thanos/War/Arena.cs

// Aggiungi 'Food' come parametro
    public byte GetLegalMoves(ushort headPosition, ushort tailPosition, ushort elementBeforeTailPosition)
    {
        byte legalMoves = 0;

        // Controlla ogni mossa (Up, Down, Left, Right)
        // UP
        var upPos = _neighborsGrid.Get(headPosition, Moves.Up);
        // Passa 'Food' a IsSquareLegal
        if (NeighborsGrid.IsValid(upPos) && IsSquareLegal(upPos, tailPosition, elementBeforeTailPosition, in Food))
            legalMoves |= Moves.Up;

        // DOWN
        var downPos = _neighborsGrid.Get(headPosition, Moves.Down);
        if (NeighborsGrid.IsValid(downPos) && IsSquareLegal(downPos, tailPosition, elementBeforeTailPosition, in Food))
            legalMoves |= Moves.Down;

        // LEFT
        var leftPos = _neighborsGrid.Get(headPosition, Moves.Left);
        if (NeighborsGrid.IsValid(leftPos) && IsSquareLegal(leftPos, tailPosition, elementBeforeTailPosition, in Food))
            legalMoves |= Moves.Left;

        // RIGHT
        var rightPos = _neighborsGrid.Get(headPosition, Moves.Right);
        if (NeighborsGrid.IsValid(rightPos) && IsSquareLegal(rightPos, tailPosition, elementBeforeTailPosition, in Food))
            legalMoves |= Moves.Right;

        return legalMoves;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsSquareLegal(ushort position, ushort tailPosition, ushort elementBeforeTailPosition, in Bitboard food)
    {
        // 1. Controlla gli ostacoli globali (Muri, Tutti i serpenti)
        var isBody = Snakes.IsSet(position);

        // 2. Se la cella è vuota, è legale.
        if (!isBody) return true;

        // 3. La cella è occupata. È la nostra coda?
        var isTail = position == tailPosition;

        // 4. Se è occupata MA NON è la nostra coda, è una collisione (muro, nemico, corpo).
        if (!isTail) return false;

        // 5. È la nostra coda. Controlliamo se è "collassata" (sovrapposta).
        //    Se la coda è nella stessa cella del pezzo prima di essa, è sovrapposta.
        if (tailPosition == elementBeforeTailPosition)
        {
            // È illegale muoversi su una coda sovrapposta.
            return false;
        }

        // 6. È la nostra coda (e non è sovrapposta).
        //    È legale muoversi qui SOLO SE non c'è cibo.
        return !food.IsSet(position);
    }

    public ushort GetNewHeadPosition(ushort head, byte move) => _neighborsGrid.Get(head, move);

    public void SimulateRandomFoodSpawn(int foodSpawnChance, int minimumFood, int area)
    {
        var currentFoodCount = Food.PopCount();
        var foodToSpawn = minimumFood - currentFoodCount;
        for (var i = 0; i < foodToSpawn; i++)
        {
            var spawnLocation = GetRandomEmptySquare(area);
            if (NeighborsGrid.IsValid(spawnLocation)) Food.Set(spawnLocation);
        }

        if (Random.Shared.Next(0, 100) < foodSpawnChance)
        {
            var spawnLocation = GetRandomEmptySquare(area);
            if (NeighborsGrid.IsValid(spawnLocation)) Food.Set(spawnLocation);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ushort GetRandomEmptySquare(int area)
    {
        for (var i = 0; i < 20; i++)
        {
            var potentialSpot = (ushort)Random.Shared.Next(0, area);
            if (Snakes.IsUnset(potentialSpot)) return potentialSpot;
        }

        return ushort.MaxValue;
    }

    public int ManhattanDistance(ushort pos1, ushort pos2)
    {
        ref readonly var coord1 = ref _conversionsMap[pos1];
        ref readonly var coord2 = ref _conversionsMap[pos2];

        return Math.Abs(coord1.X - coord2.X) + Math.Abs(coord1.Y - coord2.Y);
    }
}