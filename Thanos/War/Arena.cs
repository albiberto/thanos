using System.Runtime.CompilerServices;
using Thanos.Common;
using Thanos.Shared;
using Thanos.SourceGen; // Assumo Request sia qui
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
    /// </summary>
    public void InitializeFromRequest(in Request request, ReadOnlySpan<string> orderedIds)
    {
        // --- FIX CRITICO: Inizializzazione Strutturale ---
        // Configura Capacity e WrapMask delle code. 
        // Senza questo, su memoria zero-init, le code non funzionano (Mask=0).
        System.Initialize();

        // --- Pulizia Logica ---
        Food.Clear();
        Hazards.Clear();
        Snakes.Clear();

        // Uccidiamo logicamente i serpenti (HP=0) in attesa di ripopolarli
        // Nota: Initialize() sopra ha già resettato i puntatori delle code, 
        // ma Kill() azzera la vita e i flag.
        for (var i = 0; i < System.Count; i++) System[i].Kill();

        var board = request.Board;

        // --- Popolamento ---
        foreach (var snakeData in board.Snakes)
        {
            var snakeIndex = -1;
            
            // Mapping ID stringa -> Indice interno
            for (int i = 0; i < orderedIds.Length; i++)
            {
                if (string.Equals(orderedIds[i], snakeData.Id, StringComparison.Ordinal))
                {
                    snakeIndex = i;
                    break;
                }
            }

            if (snakeIndex != -1 && snakeIndex < System.Count)
            {
                var snake = System[snakeIndex];
                
                // Ora possiamo chiamare Initialize sul serpente perché la Queue sottostante è configurata
                snake.Initialize(snakeData);
                
                Snakes.Or(snake.Body);
            }
        }

        foreach (var foodPosition in board.Food) Food.Set(foodPosition);
        foreach (var hazardPosition in board.Hazards) Hazards.Set(hazardPosition);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CloneFrom(in Arena source)
    {
        // Copia veloce della memoria raw dei serpenti
        System.CopyFrom(in source.System);
        
        source.Food.CopyTo(Food);
        source.Hazards.CopyTo(Hazards);
        source.Snakes.CopyTo(Snakes);
    }

    // ... (Il resto dei metodi GetLegalMoves, IsSquareLegal, GetNewHeadPosition, etc. non cambia) ...
    // Li includo per completezza di compilazione se serve, altrimenti sono invariati.
    
    public byte GetLegalMoves(ushort headPosition, ushort tailPosition, ushort elementBeforeTailPosition, int heroIndex)
    {
        byte legalMoves = 0;
        var upPos = _neighborsMatrix.Get(headPosition, Moves.Up);
        if (NeighborsMatrix.IsValid(upPos) && IsSquareLegal(upPos, tailPosition, elementBeforeTailPosition, heroIndex, in Food)) legalMoves |= Moves.Up;
        var downPos = _neighborsMatrix.Get(headPosition, Moves.Down);
        if (NeighborsMatrix.IsValid(downPos) && IsSquareLegal(downPos, tailPosition, elementBeforeTailPosition, heroIndex, in Food)) legalMoves |= Moves.Down;
        var leftPos = _neighborsMatrix.Get(headPosition, Moves.Left);
        if (NeighborsMatrix.IsValid(leftPos) && IsSquareLegal(leftPos, tailPosition, elementBeforeTailPosition, heroIndex, in Food)) legalMoves |= Moves.Left;
        var rightPos = _neighborsMatrix.Get(headPosition, Moves.Right);
        if (NeighborsMatrix.IsValid(rightPos) && IsSquareLegal(rightPos, tailPosition, elementBeforeTailPosition, heroIndex, in Food)) legalMoves |= Moves.Right;
        return legalMoves;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsSquareLegal(ushort position, ushort tailPosition, ushort elementBeforeTailPosition, int heroIndex, in Bitboard food)
    {
        if (Snakes.IsSet(position))
        {
            var heroLength = System[heroIndex].Length;
            for (var i = 0; i < System.Count; i++)
            {
                if (i == heroIndex) continue;
                if (i < heroIndex && System[i].Head == position) return heroLength > System[i].Length;
            }
            if (position != tailPosition) return false;
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