using System.Runtime.CompilerServices;
using Thanos.Common;
using Thanos.Shared;
using Thanos.War.Snake;
using Thanos.War.Structures;

namespace Thanos.War.Brain;

public static class HeuristicsConstants
{
    public const float SpaceWeight = 10.5f;
    public const float HealthWeight = 0.5f;
    public const float FoodWeight = 0.6f;
    public const float TailWeight = 0.5f;

    public const float CenterBonusValue = 15.0f;
    public const float BorderPenaltyValue = -1000.0f;

    // Penalità per chi si infila in uno spazio più piccolo della sua lunghezza
    public const float SuffocationPenalty = -50000.0f;
}

public readonly struct HeuristicWeights
{
    public float Space { get; init; }
    public float Health { get; init; }
    public float Food { get; init; }
    public float Tail { get; init; }
    public float Aggression { get; init; }
    public float CenterBonus { get; init; }

    public const float BorderPenalty = -1000.0f;
    public const float SuffocationPenalty = -50000.0f;

    // --- PROFILI ---

    public static HeuristicWeights Balanced => new()
    {
        Space = 10.5f,
        Health = 0.5f,
        Food = 0.8f,
        Tail = 0.5f,
        Aggression = 1.5f,
        CenterBonus = 15.0f
    };

    public static HeuristicWeights Hungry => new()
    {
        Space = 5.0f,
        Health = 0.0f,
        Food = 4.5f,
        Tail = 0.2f,
        Aggression = 0.0f,
        CenterBonus = 5.0f
    };

    public static HeuristicWeights HeadHunter => new()
    {
        Space = 12.0f,
        Health = 0.1f,
        Food = 0.2f,
        Tail = 0.1f,
        Aggression = 10.0f,
        CenterBonus = 20.0f
    };

    public static HeuristicWeights Defensive => new()
    {
        Space = 25.0f,
        Health = 1.0f,
        Food = 0.6f,
        Tail = 2.0f,
        Aggression = -10.0f,
        CenterBonus = 0.0f
    };
}

public readonly ref struct Heuristics(SnakesSystem system, Bitboard food, Bitboard hazards, Bitboard snakes, NeighborsMatrix neighborsMatrix, CoordinatesMatrix conversionsMatrix)
{
    private readonly SnakesSystem _system = system;
    private readonly Bitboard _food = food;
    private readonly Bitboard _hazards = hazards;
    private readonly Bitboard _snakes = snakes;
    private readonly NeighborsMatrix _neighborsMatrix = neighborsMatrix;
    private readonly CoordinatesMatrix _conversionsMatrix = conversionsMatrix;

    private static readonly byte[] AllMovesArray = [Moves.Up, Moves.Down, Moves.Left, Moves.Right];

    public float Outcome(int playerIndex)
    {
        var snake = _system[playerIndex];
        if (snake.IsDead) return -1.0f;

        var othersAlive = 0;
        for (var i = 0; i < _system.Count; i++)
            if (i != playerIndex && !_system[i].IsDead)
                othersAlive++;

        if (othersAlive == 0) return 1.0f;

        return 0.0f;
    }

    /// <summary>
    /// EvaluateAll ora accetta 'isPhaseComplete'.
    /// Se false (siamo a metà turno), saremo conservativi sulla lunghezza.
    /// </summary>
    [SkipLocalsInit]
    public void EvaluateAll(Span<float> results, bool isPhaseComplete)
    {
        var area = Constants.Medium.Area;

        Span<byte> wallsMemoryCopy = stackalloc byte[_snakes.Raw.Length];
        _snakes.Raw.CopyTo(wallsMemoryCopy);
        var baseWalls = new Bitboard(wallsMemoryCopy);

        // Passiamo isPhaseComplete anche al Voronoi per coerenza (pesi cibo/spazio)
        EvaluateTerritoryAndFoodFair(area, in baseWalls, results, isPhaseComplete);

        for (var i = 0; i < _system.Count; i++)
        {
            var snake = _system[i];
            if (snake.IsDead)
            {
                results[i] = -10000.0f;
                continue;
            }

            var head = snake.Head;
            if (head >= area) continue;

            // Selezione Profilo sensibile alla Fase del Turno
            var weights = SelectProfile(in snake, i, isPhaseComplete);

            var score = 0.0f;

            score += EvaluateHealth(snake.Hp, weights.Health);
            score += EvaluateTailDistance(head, snake.Tail, weights.Tail);
            score += EvaluateCollisionsAndTraps(i, head, snake.Length, in baseWalls);

            if (weights.Aggression != 0)
            {
                // Usiamo la lunghezza conservativa anche per calcolare l'efficacia dell'aggressione
                var effectiveLen = GetConservativeLength(in snake, isPhaseComplete);
                score += EvaluateAggression(i, head, effectiveLen, weights.Aggression);
            }

            results[i] += score;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int GetConservativeLength(in WarSnake snake, bool isPhaseComplete)
    {
        // Se il turno NON è completo (gli altri devono ancora muovere) E ho appena mangiato (pending growth),
        // ignoro temporaneamente il +1 di lunghezza per non sentirmi falsamente superiore.
        if (!isPhaseComplete && snake.IsGrowthPending)
            return snake.Length - 1;

        return snake.Length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private HeuristicWeights SelectProfile(in WarSnake snake, int snakeIndex, bool isPhaseComplete)
    {
        if (snake.Hp < 35) return HeuristicWeights.Hungry;

        // Calcoliamo la lunghezza "effettiva" (conservativa se necessario)
        var myLen = GetConservativeLength(in snake, isPhaseComplete);

        var maxEnemyLen = 0;
        for (var i = 0; i < _system.Count; i++)
        {
            if (i == snakeIndex || _system[i].IsDead) continue;
            var enemyLen = _system[i].Length;
            if (enemyLen > maxEnemyLen) maxEnemyLen = enemyLen;
        }

        // Ora HeadHunter si attiva solo se siamo "Veramente" più grandi, 
        // scontando eventuali vantaggi temporanei di questo turno.
        if (myLen > maxEnemyLen && snake.Hp > 50)
            return HeuristicWeights.HeadHunter;

        if (myLen <= maxEnemyLen)
            return HeuristicWeights.Defensive;

        return HeuristicWeights.Balanced;
    }

    public float Evaluate(bool isPhaseComplete)
    {
        Span<float> results = stackalloc float[_system.Count];
        EvaluateAll(results, isPhaseComplete);
        return results[0];
    }

    // --- METODI EURISTICI ---
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private float EvaluateHealth(int health, float weight) => health * weight;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private float EvaluateCollisionsAndTraps(int snakeIndex, ushort head, int myLength, in Bitboard simulatedWalls) => Head2HeadCollision(snakeIndex, myLength, head) - PenalityTrap(head, in simulatedWalls);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private float EvaluateTailDistance(ushort head, ushort tail, float weight) => ManhattanDistance(head, tail) * weight;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private float EvaluateAggression(int myIndex, ushort myHead, int myLength, float weight)
    {
        float score = 0;
        for (var i = 0; i < _system.Count; i++)
        {
            if (i == myIndex || _system[i].IsDead) continue;
            var enemy = _system[i];

            var dist = ManhattanDistance(myHead, enemy.Head);

            if (weight > 0) // HeadHunter / Balanced (Aggressive)
            {
                // Attacca solo se sei strettamente più lungo
                if (myLength > enemy.Length)
                {
                    if (dist <= 2) score += 50.0f * weight;
                    else if (dist <= 4) score += 20.0f * weight;
                    else score += 10.0f / dist * weight;
                }
            }
            else // Defensive (weight < 0)
            {
                // Se il nemico può uccidermi (o pareggiare che è male uguale)
                if (enemy.Length >= myLength)
                {
                    // MODIFICA: Penalità DRACONIANA.
                    // Moltiplichiamo per 1000 per assicurarci che superi qualsiasi bonus spazio (Space=25).
                    // Esempio: -10 * 50 * 100 = -50.000 (Equivalente al soffocamento)

                    if (dist <= 2) score += 5000.0f * weight; // Penalità MASSIVA (-50.000 se weight è -10)
                    else if (dist <= 3) score += 1000.0f * weight; // Penalità forte per vicinanza media
                }
            }
        }

        return score;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [SkipLocalsInit]
    private void EvaluateTerritoryAndFoodFair(int area, in Bitboard walls, Span<float> results, bool isPhaseComplete)
    {
        Span<ushort> queue = stackalloc ushort[area];
        var queueHead = 0;
        var queueTail = 0;

        Span<int> owners = stackalloc int[area];
        owners.Fill(-1);

        Span<ushort> distances = stackalloc ushort[area];
        distances.Fill(ushort.MaxValue);

        for (var i = 0; i < _system.Count; i++)
        {
            var snake = _system[i];
            if (snake.IsDead) continue;
            var head = snake.Head;
            if (head >= area) continue;
            owners[head] = i;
            distances[head] = 0;
            queue[queueTail++] = head;
        }

        while (queueHead < queueTail)
        {
            var currentPos = queue[queueHead++];
            var currentOwner = owners[currentPos];
            var currentDist = distances[currentPos];

            if (currentOwner == -2) continue;
            var nextDist = (ushort)(currentDist + 1);

            foreach (var move in AllMovesArray)
            {
                var neighborPos = _neighborsMatrix.Get(currentPos, move);
                if (!NeighborsMatrix.IsValid(neighborPos) || walls.IsSet(neighborPos)) continue;

                var neighborOwner = owners[neighborPos];
                if (neighborOwner == -1)
                {
                    owners[neighborPos] = currentOwner;
                    distances[neighborPos] = nextDist;
                    queue[queueTail++] = neighborPos;
                }
                else if (neighborOwner != currentOwner && neighborOwner != -2)
                {
                    if (distances[neighborPos] == nextDist) owners[neighborPos] = -2;
                }
            }
        }

        Span<int> spaceCounts = stackalloc int[_system.Count];
        spaceCounts.Clear();

        Span<float> foodScores = stackalloc float[_system.Count];

        for (var i = 0; i < _system.Count; i++)
        {
            if (_system[i].IsDead) continue;
            var w = SelectProfile(_system[i], i, isPhaseComplete);
            foodScores[i] = (101.0f - _system[i].Hp) * w.Food;
        }

        for (var i = 0; i < area; i++)
        {
            var owner = owners[i];
            if (owner < 0) continue;

            spaceCounts[owner]++;
            if (_food.IsSet((ushort)i)) results[owner] += foodScores[owner];
        }

        for (var i = 0; i < _system.Count; i++)
        {
            if (_system[i].IsDead) continue;

            var mySpace = spaceCounts[i];
            var myLength = _system[i].Length;
            var w = SelectProfile(_system[i], i, isPhaseComplete);

            if (mySpace < myLength) results[i] += HeuristicWeights.SuffocationPenalty;
            results[i] += mySpace * w.Space;
        }
    }

    private float PenalityTrap(ushort head, in Bitboard simulatedWalls)
    {
        var openExits = 0;
        foreach (var move in AllMovesArray)
        {
            var neighbor = _neighborsMatrix.Get(head, move);
            if (NeighborsMatrix.IsValid(neighbor) && !simulatedWalls.IsSet(neighbor))
                openExits++;
        }

        return openExits switch { <= 1 => 750.0f, 2 => 200.0f, _ => 0 };
    }

    private float Head2HeadCollision(int snakeIndex, int myLength, ushort head)
    {
        for (var i = 0; i < _system.Count; i++)
        {
            if (i == snakeIndex) continue;
            var enemy = _system[i];
            if (enemy.IsDead || enemy.Length < myLength) continue;
            var enemyHead = enemy.Head;

            if (_neighborsMatrix.Get(head, Moves.Up) == enemyHead ||
                _neighborsMatrix.Get(head, Moves.Down) == enemyHead ||
                _neighborsMatrix.Get(head, Moves.Left) == enemyHead ||
                _neighborsMatrix.Get(head, Moves.Right) == enemyHead)
                return float.NegativeInfinity;
        }

        return 0.0f;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int ManhattanDistance(ushort pos1, ushort pos2)
    {
        var coord1 = _conversionsMatrix[pos1];
        var coord2 = _conversionsMatrix[pos2];
        return Math.Abs(coord1.X - coord2.X) + Math.Abs(coord1.Y - coord2.Y);
    }
}