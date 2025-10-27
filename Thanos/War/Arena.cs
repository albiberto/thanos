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

        #if DEBUG
            var logBuilder = new StringBuilder();
            logBuilder.Append($"[GetLegalMoves] Head at {headPosition}. Legal moves bitmap: {Convert.ToString(legalMoves, 2).PadLeft(4, '0')} (");

            if ((legalMoves & Moves.Up) != 0) logBuilder.Append("Up ");
            if ((legalMoves & Moves.Down) != 0) logBuilder.Append("Down ");
            if ((legalMoves & Moves.Left) != 0) logBuilder.Append("Left ");
            if ((legalMoves & Moves.Right) != 0) logBuilder.Append("Right ");
            logBuilder.Append(")");

            Console.WriteLine(logBuilder.ToString());
        #endif
        
        return legalMoves;
    }

public ushort CalculateNewTailPosition(WarSnake snake, bool ateFood)
{
    // --- BLOCCO DI LOGGING DETTAGLIATO ---
    var log = new StringBuilder();
    log.AppendLine("==================================================================");
    log.AppendLine($"[CalculateNewTailPosition] Inizio calcolo per serpente con Head:{snake.Head}, Tail:{snake.Tail}, Length:{snake.Length}");
    log.AppendLine($" -> Bit a 1 nel corpo (segmenti fisici): {snake.Body.PopCount()}");

    if (ateFood)
    {
        log.AppendLine(" -> DECISIONE: Serpente ha mangiato. La coda non si muove.");
        log.AppendLine($" -> RITORNO: Vecchia coda ({snake.Tail})");
        log.AppendLine("==================================================================");
        Console.WriteLine(log.ToString());
        return snake.Tail;
    }

    if (snake.Length <= 2)
    {
        log.AppendLine(" -> DECISIONE: Serpente corto (<=2). La nuova coda sarà la vecchia testa.");
        log.AppendLine($" -> RITORNO: Vecchia testa ({snake.Head})");
        log.AppendLine("==================================================================");
        Console.WriteLine(log.ToString());
        return snake.Head;
    }

    var oldTail = snake.Tail;
    log.AppendLine($" -> La vecchia coda si trova in posizione: {oldTail}");

    // UP
    var up = _neighborsGrid.Get(oldTail, Moves.Up);
    if (NeighborsGrid.IsValid(up))
    {
        log.Append($"   -> Controllo SU ({up}): È parte del corpo? {snake.IsOnBody(up)}");
        if (snake.IsOnBody(up))
        {
            log.AppendLine(" -> TROVATO! Questa sarà la nuova coda.");
            log.AppendLine($" -> RITORNO: {up}");
            log.AppendLine("==================================================================");
            Console.WriteLine(log.ToString());
            return up;
        }
        log.AppendLine();
    }

    // DOWN
    var down = _neighborsGrid.Get(oldTail, Moves.Down);
    if (NeighborsGrid.IsValid(down))
    {
        log.Append($"   -> Controllo GIÙ ({down}): È parte del corpo? {snake.IsOnBody(down)}");
        if (snake.IsOnBody(down))
        {
            log.AppendLine(" -> TROVATO! Questa sarà la nuova coda.");
            log.AppendLine($" -> RITORNO: {down}");
            log.AppendLine("==================================================================");
            Console.WriteLine(log.ToString());
            return down;
        }
        log.AppendLine();
    }

    // LEFT
    var left = _neighborsGrid.Get(oldTail, Moves.Left);
    if (NeighborsGrid.IsValid(left))
    {
        log.Append($"   -> Controllo SINISTRA ({left}): È parte del corpo? {snake.IsOnBody(left)}");
        if (snake.IsOnBody(left))
        {
            log.AppendLine(" -> TROVATO! Questa sarà la nuova coda.");
            log.AppendLine($" -> RITORNO: {left}");
            log.AppendLine("==================================================================");
            Console.WriteLine(log.ToString());
            return left;
        }
        log.AppendLine();
    }

    // RIGHT
    var right = _neighborsGrid.Get(oldTail, Moves.Right);
    if (NeighborsGrid.IsValid(right))
    {
        log.Append($"   -> Controllo DESTRA ({right}): È parte del corpo? {snake.IsOnBody(right)}");
        if (snake.IsOnBody(right))
        {
            log.AppendLine(" -> TROVATO! Questa sarà la nuova coda.");
            log.AppendLine($" -> RITORNO: {right}");
            log.AppendLine("==================================================================");
            Console.WriteLine(log.ToString());
            return right;
        }
        log.AppendLine();
    }

    log.AppendLine(" -> ERRORE LOGICO: Nessun segmento del corpo adiacente alla coda trovato.");
    log.AppendLine($" -> RITORNO (fallback): Vecchia coda ({oldTail})");
    log.AppendLine("==================================================================");
    Console.WriteLine(log.ToString());
    return oldTail;
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