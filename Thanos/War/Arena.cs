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
    NeighborsMatrix neighborsMatrix,
    CoordinatesMatrix conversionsMatrix)
{
    public readonly SnakesSystem System = system;
    public readonly Bitboard Food = food;
    public readonly Bitboard Hazards = hazards;
    public readonly Bitboard Snakes = snakes;

    private readonly NeighborsMatrix _neighborsMatrix = neighborsMatrix;
    private readonly CoordinatesMatrix _conversionsMatrix = conversionsMatrix;

    /// <summary>
    /// Inizializza l'Arena dallo stato della Request.
    /// Richiede l'elenco degli ID ordinati (0=Hero, 1..N=Enemies) per mappare correttamente i dati.
    /// </summary>
    public void InitializeFromRequest(in Request request, ReadOnlySpan<string> orderedIds)
    {
        Food.Clear();
        Hazards.Clear();
        Snakes.Clear();

        // Resettiamo/Uccidiamo tutti i serpenti prima di popolarli
        for (var i = 0; i < System.Count; i++) System[i].Kill();

        var board = request.Board;

        // Iteriamo sui serpenti del JSON
        foreach (var snakeData in board.Snakes)
        {
            // Troviamo l'indice corrispondente nel nostro sistema
            // L'indice nel buffer 'orderedIds' corrisponde all'indice in 'System'
            var snakeIndex = -1;
            
            for (int i = 0; i < orderedIds.Length; i++)
            {
                // Confronto Ordinal è il più veloce
                if (string.Equals(orderedIds[i], snakeData.Id, StringComparison.Ordinal))
                {
                    snakeIndex = i;
                    break;
                }
            }

            // Se troviamo l'indice (e rientra nel count attivo), inizializziamo
            if (snakeIndex != -1 && snakeIndex < System.Count)
            {
                var snake = System[snakeIndex];
                snake.Initialize(snakeData);
                Snakes.Or(snake.Body);
            }
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
        var isBody = Snakes.IsSet(position);

        if (isBody)
        {
            var heroLength = System[heroIndex].Length;

            for (var i = 0; i < System.Count; i++)
            {
                if (i == heroIndex) continue;

                // Collisione testa-a-testa solo con chi ha già mosso (indici < heroIndex)
                if (i < heroIndex)
                    if (System[i].Head == position)
                        return heroLength > System[i].Length;
            }

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