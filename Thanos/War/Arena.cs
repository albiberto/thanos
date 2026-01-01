using System.Runtime.CompilerServices;
using Thanos.Common;
using Thanos.Shared;
using Thanos.SourceGen;
using Thanos.War.Structures;

namespace Thanos.War;

public readonly ref struct Arena(SnakesSystem system, Bitboard food, Bitboard hazards, Bitboard snakes, NeighborsMatrix neighborsMatrix, CoordinatesMatrix conversionsMatrix)
{
    public readonly SnakesSystem System = system;
    
    public readonly Bitboard Food = food;
    public readonly Bitboard Hazards = hazards;
    public readonly Bitboard Snakes = snakes;

    private readonly NeighborsMatrix _neighborsMatrix = neighborsMatrix;
    private readonly CoordinatesMatrix _conversionsMatrix = conversionsMatrix;

    public void InitializeFromRequest(in Request request, ReadOnlySpan<string> orderedIds)
    {
        System.Initialize();

        Food.Clear();
        Hazards.Clear();
        Snakes.Clear();
        
        var board = request.Board;

        foreach (var snakeData in board.Snakes)
        {
            var snakeIndex = -1;
            for (var i = 0; i < orderedIds.Length; i++)
            {
                if (!string.Equals(orderedIds[i], snakeData.Id, StringComparison.Ordinal)) continue;
                
                snakeIndex = i;
                break;
            }

            if (snakeIndex == -1 || snakeIndex >= System.Count) continue;
            
            var snake = System[snakeIndex];
            snake.Initialize(snakeData);
            
            Snakes.Or(snake.Body);
        }

        foreach (var foodPosition in board.Food) Food.Set(foodPosition);
        foreach (var hazardPosition in board.Hazards) Hazards.Set(hazardPosition);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CloneFrom(in Arena source)
    {
        System.CopyFrom(in source.System);
        
        source.Food.CopyTo(Food);
        source.Hazards.CopyTo(Hazards);
        source.Snakes.CopyTo(Snakes);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte GetPlausibleMoves(int playerIndex)
    {
        var snake = System[playerIndex];
        if (snake.IsDead) return 0;

        var legalMoves = GetLegalMoves(snake.Head, snake.Tail, snake.ElementBeforeTail, playerIndex);
        if (legalMoves == 0) return 0;

        byte plausibleMoves = 0;
        var myLength = snake.Length;
        var head = snake.Head;

        if ((legalMoves & Moves.Up) != 0)
        {
            var pos = _neighborsMatrix.Get(head, Moves.Up);
            if (!IsSuicidalMove(pos, myLength, playerIndex) && !IsDeadEnd(pos)) plausibleMoves |= Moves.Up;
        }

        if ((legalMoves & Moves.Down) != 0)
        {
            var pos = _neighborsMatrix.Get(head, Moves.Down);
            if (!IsSuicidalMove(pos, myLength, playerIndex) && !IsDeadEnd(pos)) plausibleMoves |= Moves.Down;
        }

        if ((legalMoves & Moves.Left) != 0)
        {
            var pos = _neighborsMatrix.Get(head, Moves.Left);
            if (!IsSuicidalMove(pos, myLength, playerIndex) && !IsDeadEnd(pos)) plausibleMoves |= Moves.Left;
        }

        if ((legalMoves & Moves.Right) != 0)
        {
            var pos = _neighborsMatrix.Get(head, Moves.Right);
            if (!IsSuicidalMove(pos, myLength, playerIndex) && !IsDeadEnd(pos)) plausibleMoves |= Moves.Right;
        }

        return plausibleMoves != 0 ? plausibleMoves : legalMoves;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsDeadEnd(ushort position)
    {
        // Contiamo quante uscite libere ha la cella 'position'.
        // Se è 0, è un vicolo cieco (o un buco 1x1).
        // Nota: Non serve controllare "da dove vengo", perché il mio collo è già un ostacolo in Snakes.
        
        var openExits = 0;

        var u = _neighborsMatrix.Get(position, Moves.Up);
        if (NeighborsMatrix.IsValid(u) && !Snakes.IsSet(u)) openExits++;
        
        var d = _neighborsMatrix.Get(position, Moves.Down);
        if (NeighborsMatrix.IsValid(d) && !Snakes.IsSet(d)) openExits++;

        var l = _neighborsMatrix.Get(position, Moves.Left);
        if (NeighborsMatrix.IsValid(l) && !Snakes.IsSet(l)) openExits++;

        var r = _neighborsMatrix.Get(position, Moves.Right);
        if (NeighborsMatrix.IsValid(r) && !Snakes.IsSet(r)) openExits++;

        return openExits == 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsSuicidalMove(ushort potentialHead, int myLength, int myIndex)
    {
        for (var i = 0; i < System.Count; i++)
        {
            if (i == myIndex) continue;
            var enemy = System[i];
            
            if (enemy.IsDead) continue;
            
            if (enemy.Length < myLength) continue;

            var enemyHead = enemy.Head;

            if (_neighborsMatrix.Get(potentialHead, Moves.Up) == enemyHead || _neighborsMatrix.Get(potentialHead, Moves.Down) == enemyHead || _neighborsMatrix.Get(potentialHead, Moves.Left) == enemyHead || _neighborsMatrix.Get(potentialHead, Moves.Right) == enemyHead)
            {
                return true; 
            }
        }
        
        return false;
    }
    
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
        if (!Snakes.IsSet(position)) return true;
        
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