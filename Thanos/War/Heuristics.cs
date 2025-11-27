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

public readonly ref struct Heuristics(SnakesSystem system, Bitboard food, Bitboard hazards, Bitboard snakes, NeighborsGrid neighborsGrid, ReadOnlySpan<Coordinate> conversionsMap, ReadOnlySpan<float> positionalScores)
{
    private readonly SnakesSystem _system = system;
    private readonly Bitboard _food = food;
    // Hazard non usato direttamente nel Voronoi per ora, ma disponibile
    private readonly Bitboard _hazards = hazards; 
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
        int othersAlive = 0;
        for (var i = 0; i < _system.Count; i++)
        {
            if (i != playerIndex && !_system[i].IsDead) othersAlive++;
        }

        if (othersAlive == 0) return 1.0f;

        return 0.0f;
    }

    /// <summary>
    /// Valuta lo stato per TUTTI i serpenti usando un Fair Voronoi (gestione pareggi)
    /// e rilevamento trappole topologiche (spazio < lunghezza).
    /// </summary>
    [SkipLocalsInit]
    public void EvaluateAll(Span<float> results)
    {
        int area = _positionalScores.Length;

        // 1. Setup Muri
        Span<byte> wallsMemoryCopy = stackalloc byte[_snakes.Raw.Length];
        _snakes.Raw.CopyTo(wallsMemoryCopy);
        var baseWalls = new Bitboard(wallsMemoryCopy);

        // 2. Calcolo Fair Voronoi (Space & Food & Trap Detection)
        EvaluateTerritoryAndFoodFair(area, in baseWalls, results);

        // 3. Euristiche individuali (Self-preservation & Aggression)
        for (int i = 0; i < _system.Count; i++)
        {
            var snake = _system[i];
            if (snake.IsDead)
            {
                results[i] = -10000.0f; 
                continue;
            }

            var head = snake.Head;
            if (head >= area) continue;

            float score = 0.0f;

            // Statica
            score += EvaluatePositionalScore(head);
            score += EvaluateHealth(snake.HP);
            score += EvaluateTailDistance(head, snake.Tail);

            // Dinamica
            score += EvaluateCollisionsAndTraps(i, head, snake.Length, in baseWalls);

            results[i] += score;
        }
    }
    
    public float Evaluate()
    {
        Span<float> results = stackalloc float[_system.Count];
        EvaluateAll(results);
        return results[0];
    }

    // --- METODI EURISTICI PRIVATI ---

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private float EvaluatePositionalScore(ushort head) => _positionalScores[head];
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private float EvaluateHealth(int health) => health * HeuristicsConstants.HealthWeight;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private float EvaluateCollisionsAndTraps(int snakeIndex, ushort head, int myLength, in Bitboard simulatedWalls)
    {
        return Head2HeadCollision(snakeIndex, myLength, head) - PenalityTrap(head, in simulatedWalls);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private float EvaluateTailDistance(ushort head, ushort tail)
    {
        return ManhattanDistance(head, tail) * HeuristicsConstants.TailWeight;
    }

    /// <summary>
    /// Implementazione Voronoi "Fair" (Equo).
    /// Gestisce l'arrivo simultaneo su una cella: se due serpenti arrivano nello stesso step, la cella è contesa (neutral).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [SkipLocalsInit]
    private void EvaluateTerritoryAndFoodFair(int area, in Bitboard walls, Span<float> results)
    {
        // Code per BFS
        Span<ushort> queue = stackalloc ushort[area]; 
        int queueHead = 0;
        int queueTail = 0;

        // Array dei proprietari: -1 = nessuno, -2 = conteso (pareggio), 0..3 = snakeIndex
        Span<int> owners = stackalloc int[area];
        owners.Fill(-1);
        
        // Array delle distanze per gestire i pareggi
        Span<ushort> distances = stackalloc ushort[area];
        distances.Fill(ushort.MaxValue);

        // Inizializza BFS con le teste
        for (int i = 0; i < _system.Count; i++)
        {
            var snake = _system[i];
            if (snake.IsDead) continue;
            
            ushort head = snake.Head;
            if(head >= area) continue;
            
            owners[head] = i; 
            distances[head] = 0;
            queue[queueTail++] = head;
        }

        // BFS Expansion
        while (queueHead < queueTail)
        {
            ushort currentPos = queue[queueHead++];
            int currentOwner = owners[currentPos];
            ushort currentDist = distances[currentPos];

            // Se la cella di partenza era contesa (-2), non espandiamo la proprietà
            if (currentOwner == -2) continue;

            var nextDist = (ushort)(currentDist + 1);

            foreach (var move in AllMovesArray)
            {
                ushort neighborPos = _neighborsGrid.Get(currentPos, move);
                
                if (!NeighborsGrid.IsValid(neighborPos) || walls.IsSet(neighborPos)) 
                    continue;

                int neighborOwner = owners[neighborPos];

                // Caso 1: Cella non visitata
                if (neighborOwner == -1)
                {
                    owners[neighborPos] = currentOwner;
                    distances[neighborPos] = nextDist;
                    queue[queueTail++] = neighborPos;
                }
                // Caso 2: Cella già visitata, controlliamo se c'è un pareggio (distanza uguale)
                else if (neighborOwner != currentOwner && neighborOwner != -2)
                {
                    if (distances[neighborPos] == nextDist)
                    {
                        // CONFLITTO! Arrivo simultaneo.
                        // La cella diventa contesa (nessuno prende punti)
                        owners[neighborPos] = -2; 
                        // Non serve ri-accodare perché la distanza non cambia, ma lo stato ownership sì.
                        // Chiunque altro arrivi a questa distanza troverà -2 e si fermerà.
                    }
                }
            }
        }

        // --- AGGREGAZIONE E SURVIVAL INSTINCT ---
        
        Span<int> spaceCounts = stackalloc int[_system.Count];
        spaceCounts.Clear();
        
        Span<float> foodUrgencies = stackalloc float[_system.Count];
        for(int i=0; i<_system.Count; i++) 
             foodUrgencies[i] = (101.0f - _system[i].HP) * HeuristicsConstants.FoodWeight;

        for (int i = 0; i < area; i++)
        {
            int owner = owners[i];
            
            // Ignoriamo celle libere o contese
            if (owner < 0) continue; 

            spaceCounts[owner]++;

            if (_food.IsSet((ushort)i))
            {
                results[owner] += foodUrgencies[owner];
            }
        }

        for(int i=0; i<_system.Count; i++)
        {
            if (_system[i].IsDead) continue;

            int mySpace = spaceCounts[i];
            int myLength = _system[i].Length;

            // SURVIVAL INSTINCT:
            // Se lo spazio che controllo è minore della mia lunghezza, sono in trappola (soffocamento).
            // Applico una penalità enorme per insegnare al MCTS a evitare queste situazioni.
            // Nota: Usiamo una soglia leggermente permissiva (Length) perché la coda si muove.
            if (mySpace < myLength)
            {
                results[i] += HeuristicsConstants.SuffocationPenalty;
            }
            
            // Punteggio base per lo spazio
            results[i] += mySpace * HeuristicsConstants.SpaceWeight;
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