using System.Runtime.CompilerServices;
using Thanos.Common;
using Thanos.SourceGen;
using System.Text;
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
        {
            if (map.TryGetValue(snakeData.Id, out var snakeIndex))
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
    
    public byte GetLegalMoves(ushort headPosition, ushort tailPosition)
    {
        byte legalMoves = 0;
        
        // Controlla ogni mossa (Up, Down, Left, Right)
        // UP
        var upPos = _neighborsGrid.Get(headPosition, Moves.Up);
        if (NeighborsGrid.IsValid(upPos) && IsSquareLegal(upPos, tailPosition)) 
            legalMoves |= Moves.Up;
        
        // DOWN
        var downPos = _neighborsGrid.Get(headPosition, Moves.Down);
        if (NeighborsGrid.IsValid(downPos) && IsSquareLegal(downPos, tailPosition)) 
            legalMoves |= Moves.Down;
        
        // LEFT
        var leftPos = _neighborsGrid.Get(headPosition, Moves.Left);
        if (NeighborsGrid.IsValid(leftPos) && IsSquareLegal(leftPos, tailPosition)) 
            legalMoves |= Moves.Left;
        
        // RIGHT
        var rightPos = _neighborsGrid.Get(headPosition, Moves.Right);
        if (NeighborsGrid.IsValid(rightPos) && IsSquareLegal(rightPos, tailPosition)) 
            legalMoves |= Moves.Right;
        
        return legalMoves;
    }

    // AGGIUNGI QUESTO NUOVO METODO HELPER (privato)
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsSquareLegal(ushort position, ushort tailPosition)
    {
        bool isBody = Snakes.IsSet(position);
        
        // Caso 1: La casella è libera (non è corpo). È legale.
        if (!isBody) return true;

        // Caso 2: La casella è corpo, ma è la nostra coda.
        if (position == tailPosition)
        {
            // È legale SOLO SE non c'è cibo sulla coda
            // (perché se ci fosse, mangeremmo e la coda non si muoverebbe).
            return !Food.IsSet(position);
        }
        
        // Caso 3: La casella è corpo e non è la nostra coda. Non è legale.
        return false;
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