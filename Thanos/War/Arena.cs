using System.Runtime.CompilerServices;
using Thanos.Common;
using Thanos.Shared;
using Thanos.SourceGen;
using Thanos.War.Structures;

namespace Thanos.War;

public readonly ref struct Arena(
    SnakesSystem system,
    Bitboard food,
    Bitboard hazards,
    Bitboard snakes,
    Dictionary<string, int> map,
    NeighborsMatrix neighborsMatrix,
    CoordinatesMatrix conversionsMatrix)
{
    public readonly SnakesSystem System = system;
    public readonly Bitboard Food = food;
    public readonly Bitboard Hazards = hazards;
    public readonly Bitboard Snakes = snakes;

    private readonly NeighborsMatrix _neighborsMatrix = neighborsMatrix;
    private readonly CoordinatesMatrix _conversionsMatrix = conversionsMatrix;

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

    // MODIFICATO: Ora accetta 'heroIndex' per controllare chi sta muovendo
    public byte GetLegalMoves(ushort headPosition, ushort tailPosition, ushort elementBeforeTailPosition, int heroIndex)
    {
        byte legalMoves = 0;

        // UP
        var upPos = _neighborsMatrix.Get(headPosition, Moves.Up);
        if (NeighborsMatrix.IsValid(upPos) && IsSquareLegal(upPos, tailPosition, elementBeforeTailPosition, heroIndex, in Food))
            legalMoves |= Moves.Up;

        // DOWN
        var downPos = _neighborsMatrix.Get(headPosition, Moves.Down);
        if (NeighborsMatrix.IsValid(downPos) && IsSquareLegal(downPos, tailPosition, elementBeforeTailPosition, heroIndex, in Food))
            legalMoves |= Moves.Down;

        // LEFT
        var leftPos = _neighborsMatrix.Get(headPosition, Moves.Left);
        if (NeighborsMatrix.IsValid(leftPos) && IsSquareLegal(leftPos, tailPosition, elementBeforeTailPosition, heroIndex, in Food))
            legalMoves |= Moves.Left;

        // RIGHT
        var rightPos = _neighborsMatrix.Get(headPosition, Moves.Right);
        if (NeighborsMatrix.IsValid(rightPos) && IsSquareLegal(rightPos, tailPosition, elementBeforeTailPosition, heroIndex, in Food))
            legalMoves |= Moves.Right;

        return legalMoves;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsSquareLegal(ushort position, ushort tailPosition, ushort elementBeforeTailPosition, int heroIndex, in Bitboard food)
    {
        // 1. Controlla se la casella è occupata
        var isBody = Snakes.IsSet(position);

        if (isBody)
        {
            // FIX INTELLIGENTE: Gestione Collisioni Testa-a-Testa
            // Controlliamo se stiamo colpendo la testa di un nemico che HA GIÀ MOSSO.

            var heroLength = System[heroIndex].Length;

            for (var i = 0; i < System.Count; i++)
            {
                if (i == heroIndex) continue; // Salta noi stessi

                // Controlliamo solo i nemici con indice < heroIndex.
                // In un ciclo Round-Robin 0->1->2->3, questi sono quelli che hanno già aggiornato la loro posizione.
                // Colpire la loro testa ora simula una collisione reale.
                // Colpire la testa di chi NON ha mosso (i > heroIndex) significa colpire il collo -> Suicidio.
                if (i < heroIndex)
                    if (System[i].Head == position)
                        // È un testa-a-testa valido. Applichiamo la tua logica:
                        // Se sono più lungo -> VALIDA (Kill).
                        // Se sono più corto o uguale -> INVALIDA (Suicidio/Pareggio da evitare).
                        return heroLength > System[i].Length;
            }

            // Se non è una testa "uccidibile", applichiamo la logica standard (coda/muro)
            var isTail = position == tailPosition;
            if (!isTail) return false;

            if (tailPosition == elementBeforeTailPosition) return false;
            return !food.IsSet(position);
        }

        return true;
    }

    public ushort GetNewHeadPosition(ushort head, byte move) => _neighborsMatrix.Get(head, move);

    public void SimulateRandomFoodSpawn(int foodSpawnChance, int minimumFood, int area)
    {
        var currentFoodCount = Food.PopCount();
        var foodToSpawn = minimumFood - currentFoodCount;
        for (var i = 0; i < foodToSpawn; i++)
        {
            var spawnLocation = GetRandomEmptySquare(area);
            if (NeighborsMatrix.IsValid(spawnLocation)) Food.Set(spawnLocation);
        }

        if (Random.Shared.Next(0, 100) < foodSpawnChance)
        {
            var spawnLocation = GetRandomEmptySquare(area);
            if (NeighborsMatrix.IsValid(spawnLocation)) Food.Set(spawnLocation);
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
}