using System.Runtime.CompilerServices;
using Thanos.Common;
using Thanos.SourceGen;
using System.Text; // Aggiunto per StringBuilder

namespace Thanos.War;

/// <summary>
///     Rappresenta una singola istanza di gioco. È il cervello che orchestra la logica.
/// </summary>
public readonly ref struct Arena(
    SnakesSystem system, 
    Bitboard food, 
    Bitboard hazards, 
    Bitboard snakes, 
    NeighborsGrid neighborsGrid, 
    Dictionary<string, int> map,
    ReadOnlySpan<Coordinate> conversionsMap)
{
    public readonly SnakesSystem System = system;

    public readonly Bitboard Food = food;
    public readonly Bitboard Hazards = hazards;
    public readonly Bitboard Snakes = snakes;

    private readonly NeighborsGrid _neighborsGrid = neighborsGrid;
    private readonly ReadOnlySpan<Coordinate> _conversionsMap = conversionsMap;

    public int PlayerToMoveIndex
    {
        get => System.PlayerToMoveIndex;
        set => System.PlayerToMoveIndex = value;
    }

    public void InitializeFromRequest(in Request request)
    {
        // LOGGING: Annuncia l'inizializzazione
        Console.WriteLine($"[Arena] Initializing from request for turn {request.Turn}. Snakes: {request.Board.Snakes.Length}, Food: {request.Board.Food.Length}.");

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
                snake.Initialize(snakeData.Health, snakeData.Body);
                Snakes.Or(snake.Body);
            }
        }

        foreach (var foodPosition in board.Food) Food.Set(foodPosition);
        foreach (var hazardPosition in board.Hazards) Hazards.Set(hazardPosition);
        
        this.PlayerToMoveIndex = 0;
    }

    public void CloneFrom(in Arena source)
    {
        // LOGGING: Annuncia la clonazione
        Console.WriteLine("[Arena] Cloning state from another Arena.");
        
        source.System.Raw.CopyTo(System.Raw);
        this.PlayerToMoveIndex = source.PlayerToMoveIndex;

        source.Food.CopyTo(this.Food);
        source.Hazards.CopyTo(this.Hazards);
        source.Snakes.CopyTo(this.Snakes);
    }
    
    private WarSnake Me => System.Me;
    
    public byte GetLegalMoves(ushort headPosition)
    {
        byte legalMoves = 0;
        var upPos = _neighborsGrid.Get(headPosition, Moves.Up);
        if (NeighborsGrid.IsValid(upPos) && !Snakes.IsSet(upPos)) legalMoves |= Moves.Up;
        
        var downPos = _neighborsGrid.Get(headPosition, Moves.Down);
        if (NeighborsGrid.IsValid(downPos) && !Snakes.IsSet(downPos)) legalMoves |= Moves.Down;
        
        var leftPos = _neighborsGrid.Get(headPosition, Moves.Left);
        if (NeighborsGrid.IsValid(leftPos) && !Snakes.IsSet(leftPos)) legalMoves |= Moves.Left;
        
        var rightPos = _neighborsGrid.Get(headPosition, Moves.Right);
        if (NeighborsGrid.IsValid(rightPos) && !Snakes.IsSet(rightPos)) legalMoves |= Moves.Right;

        // LOGGING: Mostra le mosse legali calcolate
        var logBuilder = new StringBuilder();
        logBuilder.Append($"[GetLegalMoves] Head at {headPosition}. Legal moves bitmap: {Convert.ToString(legalMoves, 2).PadLeft(4, '0')} (");
        if ((legalMoves & Moves.Up) != 0) logBuilder.Append("Up ");
        if ((legalMoves & Moves.Down) != 0) logBuilder.Append("Down ");
        if ((legalMoves & Moves.Left) != 0) logBuilder.Append("Left ");
        if ((legalMoves & Moves.Right) != 0) logBuilder.Append("Right ");
        logBuilder.Append(")");
        Console.WriteLine(logBuilder.ToString());
        
        return legalMoves;
    }

    public void ApplySingleMove(int snakeIndex, byte move, Dictionary<int, ushort> newHeads, Dictionary<int, byte> combinedMoves)
    {
        var snake = System[snakeIndex];
        if (snake.IsDead) return;
        var newHead = _neighborsGrid.Get(snake.Head, move);
        newHeads[snakeIndex] = newHead;
        var hasEaten = Food.IsSet(newHead);
        var damage = Hazards.IsSet(newHead) ? 10 : 1;
        var newTail = CalculateNewTailPosition(snake, hasEaten);
        snake.UpdateAfterMove(newHead, newTail, hasEaten, damage);
        Snakes.Xor(snake.Body);
    }

    public ushort CalculateNewTailPosition(WarSnake snake, bool ateFood)
    {
        if (ateFood) return snake.Tail;
        if (snake.Length <= 2) return snake.Head;
        var oldTail = snake.Tail;
        var up = _neighborsGrid.Get(oldTail, Moves.Up);
        if (NeighborsGrid.IsValid(up) && snake.IsOnBody(up)) return up;
        var down = _neighborsGrid.Get(oldTail, Moves.Down);
        if (NeighborsGrid.IsValid(down) && snake.IsOnBody(down)) return down;
        var left = _neighborsGrid.Get(oldTail, Moves.Left);
        if (NeighborsGrid.IsValid(left) && snake.IsOnBody(left)) return left;
        var right = _neighborsGrid.Get(oldTail, Moves.Right);
        if (NeighborsGrid.IsValid(right) && snake.IsOnBody(right)) return right;
        return oldTail;
    }

    public ushort GetNewHeadPosition(ushort head, byte move) => _neighborsGrid.Get(head, move);
    public bool IsValidPosition(ushort pos) => NeighborsGrid.IsValid(pos);

    public void SimulateRandomFoodSpawn(int foodSpawnChance, int minimumFood)
    {
        var currentFoodCount = Food.PopCount();
        var foodToSpawn = minimumFood - currentFoodCount;
        for (var i = 0; i < foodToSpawn; i++)
        {
            var spawnLocation = GetRandomEmptySquare();
            if (NeighborsGrid.IsValid(spawnLocation)) Food.Set(spawnLocation);
        }
        if (Random.Shared.Next(0, 100) < foodSpawnChance)
        {
            var spawnLocation = GetRandomEmptySquare();
            if (NeighborsGrid.IsValid(spawnLocation)) Food.Set(spawnLocation);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ushort GetRandomEmptySquare()
    {
        for (var i = 0; i < 20; i++)
        {
            var potentialSpot = (ushort)Random.Shared.Next(0, _neighborsGrid.Area);
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