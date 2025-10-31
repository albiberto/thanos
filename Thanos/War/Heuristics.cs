// Thanos/War/Heuristics.cs

using System.Numerics;
using System.Runtime.CompilerServices;
using Thanos.Common;
using Thanos.PreWarm;
using Thanos.SourceGen;
using Thanos.War.Structures;

namespace Thanos.War;

public static class HeuristicsConstants
{
    public const float SpaceWeight = 10.5f;
    public const float HealthWeight = 0.5f;
    public const float FoodWeight = 0.6f;
    public const float TailWeight = 0.5f; // <-- NUOVA COSTANTE
    public const float CenterBonusValue = 15.0f;
    public const float BorderPenaltyValue = -100.0f;
}

public readonly ref struct Heuristics(SnakesSystem system, Bitboard food, Bitboard hazards, Bitboard snakes, NeighborsGrid neighborsGrid, ReadOnlySpan<Coordinate> conversionsMap, ReadOnlySpan<float> positionalScores)
{
    private readonly SnakesSystem _system = system;
    private readonly Bitboard _food = food;
    private readonly Bitboard _hazards = hazards;
    private readonly Bitboard _snakes = snakes;
    private readonly NeighborsGrid _neighborsGrid = neighborsGrid;
    private readonly ReadOnlySpan<Coordinate> _conversionsMap = conversionsMap;
    private readonly ReadOnlySpan<float> _positionalScores = positionalScores;

    private static readonly byte[] AllMovesArray = [Moves.Up, Moves.Down, Moves.Left, Moves.Right];

    public float Outcome()
    {
        var me = _system.Me;
        if (me.IsDead) return -1.0f;

        for (var i = 1; i < _system.Count; i++)
            if (!_system[i].IsDead)
                return 0.0f;

        return 1.0f;
    }

    /// <summary>
    /// Metodo principale rifattorizzato. Ora orchestra le chiamate ai metodi privati.
    /// </summary>
    public float Evaluate()
    {
        var me = _system.Me;
        if (me.IsDead) return float.NegativeInfinity;

        var head = me.Head;
        int area = _positionalScores.Length; 
        if (head >= area) return float.NegativeInfinity;

        var myLength = me.Length;
        var health = me.HP;
        var score = 0.0f;

        // --- 1. Euristiche "Statiche" (Stato attuale) ---
        score += EvaluatePositionalScore(head);
        score += EvaluateHealth(health);
        score += EvaluateCollisionsAndTraps(head, myLength);
        score += EvaluateTailDistance(me.Head, me.Tail); // <-- NUOVA EURISTICA

        // --- 2. Euristiche "Simulate" (Stato futuro) ---
        
        // Creiamo una copia dei muri per simulare il movimento della coda
        Span<byte> wallsMemoryCopy = stackalloc byte[_snakes.Raw.Length];
        _snakes.Raw.CopyTo(wallsMemoryCopy);
        var simulatedWalls = new Bitboard(wallsMemoryCopy);
        simulatedWalls.Unset(me.Tail); 

        // Calcola spazio (Voronoi) E cibo conteso in un unico passaggio
        score += EvaluateTerritoryAndFood(area, in simulatedWalls, myLength, health);

        return score;
    }

    // --- METODI EURISTICI PRIVATI ---

    /// <summary>
    /// 1. EURISTICA: Valuta il bonus/malus per la posizione sulla scacchiera.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private float EvaluatePositionalScore(ushort head)
    {
        return _positionalScores[head];
    }
    
    /// <summary>
    /// 2. EURISTICA: Valuta la nostra salute attuale.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private float EvaluateHealth(int health)
    {
        return health * HeuristicsConstants.HealthWeight;
    }

    /// <summary>
    /// 3. EURISTICA: Applica penalità per trappole e potenziali collisioni H2H.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private float EvaluateCollisionsAndTraps(ushort head, int myLength)
    {
        return -Head2HeadCollision(myLength, head) - PenalityTrap(head);
    }
    
    /// <summary>
    /// 4. EURISTICA: Incentiva l'aumento della distanza tra testa e coda ("srotolamento").
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private float EvaluateTailDistance(ushort head, ushort tail)
    {
        int distance = ManhattanDistance(head, tail);
        return distance * HeuristicsConstants.TailWeight;
    }

    /// <summary>
    /// 5. EURISTICA (UNIFICATA): Calcola il territorio Voronoi (spazio) E il punteggio del cibo conteso.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [SkipLocalsInit]
    private float EvaluateTerritoryAndFood(int area, in Bitboard walls, int myLength, int myHealth)
    {
        Span<ushort> queue = stackalloc ushort[512]; 
        int queueHead = 0;
        int queueTail = 0;

        Span<int> owners = stackalloc int[area];
        owners.Fill(-1);

        for (int i = 0; i < _system.Count; i++)
        {
            var snake = _system[i];
            if (snake.IsDead) continue;
            ushort head = snake.Head;
            if(head >= area) continue; 
            owners[head] = i; 
            queue[queueTail++] = head;
        }

        while (queueHead < queueTail)
        {
            ushort currentPos = queue[queueHead++];
            int currentOwner = owners[currentPos];

            foreach (var move in AllMovesArray)
            {
                ushort neighborPos = _neighborsGrid.Get(currentPos, move);
                if (NeighborsGrid.IsValid(neighborPos) && 
                    !walls.IsSet(neighborPos) &&  
                    owners[neighborPos] == -1)
                {
                    owners[neighborPos] = currentOwner;
                    if (queueTail < queue.Length) 
                    {
                        queue[queueTail++] = neighborPos;
                    }
                }
            }
        }

        int mySpace = 0;
        int enemySpace = 0; 
        float foodScore = 0.0f;
        
        var foodUrgency = (101.0f - myHealth) * HeuristicsConstants.FoodWeight;
        var foodBitboard = _food.Buffer; 

        for (int i = 0; i < area; i++)
        {
            int owner = owners[i];
            
            if (owner == 0) mySpace++;
            else if (owner > 0) enemySpace++;
            
            if (_food.IsSet((ushort)i))
            {
                if (owner == 0)
                {
                    foodScore += foodUrgency; // Bonus
                }
                else if (owner > 0)
                {
                    if (_system[owner].Length >= myLength)
                    {
                        foodScore -= foodUrgency; // Penalità
                    }
                }
            }
        }
        
        float spaceScore = (mySpace - enemySpace) * HeuristicsConstants.SpaceWeight;
        return spaceScore + foodScore;
    }

    // --- METODI HELPER ---

    private float PenalityTrap(ushort head)
    {
        var openExits = 0;
        var currentWalls = _snakes;
        foreach (var move in AllMovesArray)
        {
            var neighbor = _neighborsGrid.Get(head, move);
            if (NeighborsGrid.IsValid(neighbor) && !currentWalls.IsSet(neighbor)) openExits++;
        }

        return openExits switch
        {
            <= 1 => 750.0f,
            2 => 200.0f,
            _ => 0
        };
    }

    private float Head2HeadCollision(int myLength, ushort head)
    {
        for (var i = 1; i < _system.Count; i++)
        {
            var enemy = _system[i];
            if (enemy.IsDead || enemy.Length < myLength) continue;

            var enemyHead = enemy.Head;
            if (_neighborsGrid.Get(head, Moves.Up) == enemyHead ||
                _neighborsGrid.Get(head, Moves.Down) == enemyHead ||
                _neighborsGrid.Get(head, Moves.Left) == enemyHead ||
                _neighborsGrid.Get(head, Moves.Right) == enemyHead)
                return 25000.0f;
        }

        return 0.0f;
    }
    
    /// <summary>
    /// Helper per la distanza di Manhattan, necessario per 'EvaluateTailDistance'.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int ManhattanDistance(ushort pos1, ushort pos2)
    {
        ref readonly var coord1 = ref _conversionsMap[pos1];
        ref readonly var coord2 = ref _conversionsMap[pos2];
        return Abs(coord1.X - coord2.X) + Abs(coord1.Y - coord2.Y);
    }

    /// <summary>
    /// Helper per il valore assoluto, necessario per 'ManhattanDistance'.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int Abs(int n)
    {
        var mask = n >> 31;
        return (n + mask) ^ mask;
    }
}