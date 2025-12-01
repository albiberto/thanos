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
    public const float TailWeight = 0.5f;
    
    public const float CenterBonusValue = 15.0f;
    public const float BorderPenaltyValue = -1000.0f;
    
    // Penalità per chi si infila in uno spazio più piccolo della sua lunghezza
    public const float SuffocationPenalty = -50000.0f; 
}

// 1. Rimuoviamo le costanti statiche e creiamo una struttura dati per i pesi
public readonly struct HeuristicWeights
{
    public float Space { get; init; }
    public float Health { get; init; }
    public float Food { get; init; }
    public float Tail { get; init; }
    public float Aggression { get; init; } // Nuovo parametro per l'HeadHunter
    public float CenterBonus { get; init; }
    
    // Penalità fisse (possono rimanere costanti o diventare dinamiche se serve)
    public const float BorderPenalty = -1000.0f;
    public const float SuffocationPenalty = -50000.0f;

    // --- PROFILI PREDEFINITI ---

    // 1. Balanced: Comportamento standard inizio partita
    public static HeuristicWeights Balanced => new()
    {
        Space = 10.5f,
        Health = 0.5f,
        Food = 0.8f, // Leggermente aumentato per incoraggiare la crescita early game
        Tail = 0.5f,
        Aggression = 2.0f,
        CenterBonus = 15.0f
    };

    // 2. Hungry (Starving): Quando HP < 30 o siamo molto piccoli
    public static HeuristicWeights Hungry => new()
    {
        Space = 5.0f,      // Meno interesse per lo spazio
        Health = 0.0f,     // La salute è già inclusa nel peso cibo dinamico, ma lo azzeriamo per non interferire
        Food = 4.5f,       // PRIORITÀ MASSIMA AL CIBO
        Tail = 0.2f,
        Aggression = 0.0f, // Non rischiare scontri quando hai fame
        CenterBonus = 5.0f
    };

    // 3. HeadHunter (Predator): Quando siamo i più grandi e in salute
    public static HeuristicWeights HeadHunter => new()
    {
        Space = 12.0f,     // Controlla il territorio per soffocare
        Health = 0.1f,     // La salute conta poco, siamo grossi
        Food = 0.2f,       // Mangia solo se capita
        Tail = 0.1f,
        Aggression = 8.0f, // Caccia le teste nemiche!
        CenterBonus = 20.0f // Domina il centro
    };

    // 4. Defensive (Survival): Quando siamo intrappolati o in svantaggio
    public static HeuristicWeights Defensive => new()
    {
        Space = 25.0f,     // MASSIMA priorità allo spazio vitale
        Health = 1.0f,     // Mantieniti vivo
        Food = 0.5f,
        Tail = 2.0f,       // Segui la coda per sicurezza
        Aggression = -5.0f, // Evita i nemici
        CenterBonus = 0.0f // Non rischiare il centro
    };
}

public readonly ref struct Heuristics(SnakesSystem system, Bitboard food, Bitboard hazards, Bitboard snakes, NeighborsGrid neighborsGrid, ReadOnlySpan<Coordinate> conversionsMap, ReadOnlySpan<float> positionalScores)
{
    private readonly SnakesSystem _system = system;
    private readonly Bitboard _food = food;
    private readonly Bitboard _hazards = hazards; // Disponibile per future logiche Hazard
    private readonly Bitboard _snakes = snakes;
    private readonly NeighborsGrid _neighborsGrid = neighborsGrid;
    private readonly ReadOnlySpan<Coordinate> _conversionsMap = conversionsMap;
    private readonly ReadOnlySpan<float> _positionalScores = positionalScores;

    private static readonly byte[] AllMovesArray = [Moves.Up, Moves.Down, Moves.Left, Moves.Right];

    public float Outcome(int playerIndex)
    {
        var snake = _system[playerIndex];
        if (snake.IsDead) return -1.0f;

        // Se sono l'unico vivo, ho vinto
        var othersAlive = 0;
        for (var i = 0; i < _system.Count; i++)
        {
            if (i != playerIndex && !_system[i].IsDead) othersAlive++;
        }

        if (othersAlive == 0) return 1.0f;

        return 0.0f;
    }

    /// <summary>
    /// Valuta lo stato usando profili dinamici per ogni serpente.
    /// </summary>
    [SkipLocalsInit]
    public void EvaluateAll(Span<float> results)
    {
        var area = _positionalScores.Length;

        // 1. Setup Muri
        Span<byte> wallsMemoryCopy = stackalloc byte[_snakes.Raw.Length];
        _snakes.Raw.CopyTo(wallsMemoryCopy);
        var baseWalls = new Bitboard(wallsMemoryCopy);

        // 2. Calcoliamo i profili dinamici per OGNI serpente
        //    (Nota: potremmo ottimizzare calcolando solo il nostro, ma per il Fair Voronoi serve uniformità o logica dedicata)
        //    Per ora usiamo il profilo "Balanced" per il Voronoi generale, ma applichiamo pesi specifici dopo.
        
        // Calcolo Fair Voronoi (Space & Food & Trap Detection)
        // Usiamo pesi base per il Voronoi score, poi li rifiniamo.
        EvaluateTerritoryAndFoodFair(area, in baseWalls, results);

        // 3. Euristiche individuali con PROFILI DINAMICI
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

            // --- SELEZIONE DINAMICA DEL PROFILO ---
            var weights = SelectProfile(in snake, i);
            
            var score = 0.0f;

            // Statica
            score += EvaluatePositionalScore(head, weights.CenterBonus);
            score += EvaluateHealth(snake.HP, weights.Health);
            score += EvaluateTailDistance(head, snake.Tail, weights.Tail);

            // Dinamica
            score += EvaluateCollisionsAndTraps(i, head, snake.Length, in baseWalls);
            
            // Aggressione (HeadHunter logic)
            if (weights.Aggression > 0)
            {
                score += EvaluateAggression(i, head, snake.Length, weights.Aggression);
            }

            results[i] += score;
        }
    }
    
    // --- LOGICA DI SELEZIONE DEL PROFILO ---
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private HeuristicWeights SelectProfile(in WarSnake snake, int snakeIndex)
    {
        // 1. Hungry Mode: Se stiamo morendo di fame
        if (snake.HP < 35) 
            return HeuristicWeights.Hungry;

        // 2. Analisi Dominio: Sono il serpente più lungo?
        var amIBiggest = true;
        var maxEnemyLen = 0;
        
        for(var i=0; i < _system.Count; i++)
        {
            if (i == snakeIndex || _system[i].IsDead) continue;
            var enemyLen = _system[i].Length;
            if (enemyLen >= snake.Length) amIBiggest = false;
            if (enemyLen > maxEnemyLen) maxEnemyLen = enemyLen;
        }

        // 3. HeadHunter Mode: Se sono il più grande e ho salute decente
        if (amIBiggest && snake.HP > 50)
            return HeuristicWeights.HeadHunter;

        // 4. Defensive Mode: Se c'è un nemico molto più grande vicino o ho poca mappa (semplificato)
        //    (Qui potremmo integrare i dati del Voronoi se fossero disponibili prima, ma per velocità usiamo Length)
        if (!amIBiggest && (maxEnemyLen - snake.Length) >= 2)
             return HeuristicWeights.Defensive;

        // 5. Default
        return HeuristicWeights.Balanced;
    }

    public float Evaluate()
    {
        Span<float> results = stackalloc float[_system.Count];
        EvaluateAll(results);
        return results[0];
    }

    // --- METODI EURISTICI PRIVATI ---

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private float EvaluatePositionalScore(ushort head, float weight) => _positionalScores[head] * (weight / 15.0f); // Normalizziamo rispetto al vecchio default
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private float EvaluateHealth(int health, float weight) => health * weight;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private float EvaluateCollisionsAndTraps(int snakeIndex, ushort head, int myLength, in Bitboard simulatedWalls)
    {
        return Head2HeadCollision(snakeIndex, myLength, head) - PenalityTrap(head, in simulatedWalls);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private float EvaluateTailDistance(ushort head, ushort tail, float weight)
    {
        return ManhattanDistance(head, tail) * weight;
    }
    
    // NUOVO: Logica HeadHunter
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private float EvaluateAggression(int myIndex, ushort myHead, int myLength, float weight)
    {
        float score = 0;
        for (var i = 0; i < _system.Count; i++)
        {
            if (i == myIndex || _system[i].IsDead) continue;
            var enemy = _system[i];
            
            // Caccia solo chi puoi uccidere
            if (myLength > enemy.Length)
            {
                var dist = ManhattanDistance(myHead, enemy.Head);
                
                // Bonus massiccio se siamo vicini alla testa di una preda
                if (dist <= 2) score += 50.0f * weight; 
                else if (dist <= 4) score += 20.0f * weight;
                else score += (10.0f / dist) * weight;
            }
        }
        return score;
    }

    /// <summary>
    /// Voronoi Fair (Invariato nella logica di calcolo, ma usa pesi base per l'aggregazione)
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [SkipLocalsInit]
    private void EvaluateTerritoryAndFoodFair(int area, in Bitboard walls, Span<float> results)
    {
        // Code per BFS
        Span<ushort> queue = stackalloc ushort[area]; 
        var queueHead = 0;
        var queueTail = 0;

        Span<int> owners = stackalloc int[area];
        owners.Fill(-1);
        
        Span<ushort> distances = stackalloc ushort[area];
        distances.Fill(ushort.MaxValue);

        // Init BFS
        for (var i = 0; i < _system.Count; i++)
        {
            var snake = _system[i];
            if (snake.IsDead) continue;
            var head = snake.Head;
            if(head >= area) continue;
            owners[head] = i; 
            distances[head] = 0;
            queue[queueTail++] = head;
        }

        // BFS Expansion
        while (queueHead < queueTail)
        {
            var currentPos = queue[queueHead++];
            var currentOwner = owners[currentPos];
            var currentDist = distances[currentPos];

            if (currentOwner == -2) continue;
            var nextDist = (ushort)(currentDist + 1);

            foreach (var move in AllMovesArray)
            {
                var neighborPos = _neighborsGrid.Get(currentPos, move);
                if (!NeighborsGrid.IsValid(neighborPos) || walls.IsSet(neighborPos)) continue;

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

        // --- AGGREGAZIONE CON PESI DINAMICI "AL VOLO" ---
        
        Span<int> spaceCounts = stackalloc int[_system.Count];
        spaceCounts.Clear();
        
        // Calcolo FoodScore specifico per ogni serpente basato sul suo profilo
        Span<float> foodScores = stackalloc float[_system.Count];
        
        // Qui calcoliamo i profili "al volo" solo per determinare il peso del cibo e spazio nel Voronoi
        for(var i=0; i < _system.Count; i++)
        {
            if (_system[i].IsDead) continue;
            var w = SelectProfile(_system[i], i);
            
            // Formula dinamica per il cibo: Più ho fame, più il cibo vale (esponenzialmente)
            foodScores[i] = (101.0f - _system[i].HP) * w.Food; 
        }

        for (var i = 0; i < area; i++)
        {
            var owner = owners[i];
            if (owner < 0) continue; 

            spaceCounts[owner]++;

            if (_food.IsSet((ushort)i))
            {
                results[owner] += foodScores[owner];
            }
        }

        for(var i=0; i<_system.Count; i++)
        {
            if (_system[i].IsDead) continue;

            var mySpace = spaceCounts[i];
            var myLength = _system[i].Length;
            
            var w = SelectProfile(_system[i], i);

            if (mySpace < myLength)
            {
                results[i] += HeuristicWeights.SuffocationPenalty;
            }
            
            results[i] += mySpace * w.Space;
        }
    }

    private float PenalityTrap(ushort head, in Bitboard simulatedWalls)
    {
        var openExits = 0;
        foreach (var move in AllMovesArray)
        {
            var neighbor = _neighborsGrid.Get(head, move);
            if (NeighborsGrid.IsValid(neighbor) && !simulatedWalls.IsSet(neighbor)) 
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
            
            if (_neighborsGrid.Get(head, Moves.Up) == enemyHead ||
                _neighborsGrid.Get(head, Moves.Down) == enemyHead ||
                _neighborsGrid.Get(head, Moves.Left) == enemyHead ||
                _neighborsGrid.Get(head, Moves.Right) == enemyHead)
                return float.NegativeInfinity;
        }
        return 0.0f;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int ManhattanDistance(ushort pos1, ushort pos2)
    {
        ref readonly var coord1 = ref _conversionsMap[pos1];
        ref readonly var coord2 = ref _conversionsMap[pos2];
        return Math.Abs(coord1.X - coord2.X) + Math.Abs(coord1.Y - coord2.Y);
    }
}