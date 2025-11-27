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
    /// Valuta lo stato per TUTTI i serpenti e riempie il buffer.
    /// Molto più efficiente che chiamare Evaluate() N volte.
    /// </summary>
    public void EvaluateAll(Span<float> results)
    {
        // 1. Setup Bitboard Muri (comune a tutti)
        int area = _positionalScores.Length;
        Span<byte> wallsMemoryCopy = stackalloc byte[_snakes.Raw.Length];
        _snakes.Raw.CopyTo(wallsMemoryCopy);
        var baseWalls = new Bitboard(wallsMemoryCopy);

        // 2. Calcolo Voronoi Globale (Space & Food per tutti in una passata)
        // Questo è pesante, quindi lo facciamo una volta sola e distribuiamo i risultati
        EvaluateTerritoryAndFoodGlobal(area, in baseWalls, results);

        // 3. Aggiungi euristiche individuali
        for (int i = 0; i < _system.Count; i++)
        {
            var snake = _system[i];
            if (snake.IsDead)
            {
                results[i] = -10000.0f; // Penalità morte base (oltre all'outcome)
                continue;
            }

            var head = snake.Head;
            if (head >= area) continue;

            float score = 0.0f;

            // Statica
            score += EvaluatePositionalScore(head);
            score += EvaluateHealth(snake.HP);
            score += EvaluateTailDistance(head, snake.Tail);

            // Dinamica (Collisioni)
            // Passiamo 'i' come snakeIndex per evitare di collidere con noi stessi
            score += EvaluateCollisionsAndTraps(i, head, snake.Length, in baseWalls);

            // Somma al risultato parziale del territorio
            results[i] += score;
        }
    }
    
    // Manteniamo il metodo Evaluate() singolo per compatibilità o test, ma ora punta a EvaluateAll logicamente
    public float Evaluate()
    {
        // Implementazione legacy per P0, se serve ancora
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
        // Passiamo snakeIndex per escludere noi stessi dal controllo Head2Head
        return Head2HeadCollision(snakeIndex, myLength, head) - PenalityTrap(head, in simulatedWalls);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private float EvaluateTailDistance(ushort head, ushort tail)
    {
        return ManhattanDistance(head, tail) * HeuristicsConstants.TailWeight;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [SkipLocalsInit]
    private void EvaluateTerritoryAndFoodGlobal(int area, in Bitboard walls, Span<float> results)
    {
        // Implementazione Voronoi Multi-Sorgente
        Span<ushort> queue = stackalloc ushort[area]; // Coda BFS grande quanto l'area max
        int queueHead = 0;
        int queueTail = 0;

        Span<int> owners = stackalloc int[area];
        owners.Fill(-1);
        
        // Inizializza BFS con le teste di tutti i serpenti vivi
        for (int i = 0; i < _system.Count; i++)
        {
            var snake = _system[i];
            if (snake.IsDead) continue;
            
            ushort head = snake.Head;
            if(head >= area) continue;
            
            owners[head] = i; 
            queue[queueTail++] = head;
        }

        // BFS Expansion
        while (queueHead < queueTail)
        {
            ushort currentPos = queue[queueHead++];
            int currentOwner = owners[currentPos];

            foreach (var move in AllMovesArray)
            {
                ushort neighborPos = _neighborsGrid.Get(currentPos, move);
                
                // Muri: Qui usiamo 'walls' che contiene TUTTI i corpi.
                if (NeighborsGrid.IsValid(neighborPos) && 
                    !walls.IsSet(neighborPos) && 
                    owners[neighborPos] == -1)
                {
                    owners[neighborPos] = currentOwner;
                    queue[queueTail++] = neighborPos;
                }
            }
        }

        // Aggregazione Punteggi
        Span<int> spaceCounts = stackalloc int[_system.Count];
        spaceCounts.Clear();
        
        // Calcola urgenza cibo per tutti
        Span<float> foodUrgencies = stackalloc float[_system.Count];
        for(int i=0; i<_system.Count; i++) 
             foodUrgencies[i] = (101.0f - _system[i].HP) * HeuristicsConstants.FoodWeight;

        for (int i = 0; i < area; i++)
        {
            int owner = owners[i];
            if (owner == -1) continue;

            spaceCounts[owner]++;

            if (_food.IsSet((ushort)i))
            {
                // Se possiedo il cibo, aggiungo punti.
                results[owner] += foodUrgencies[owner];
            }
        }

        // Aggiungi punteggio spazio
        for(int i=0; i<_system.Count; i++)
        {
            if (_system[i].IsDead) continue;
            // Confronto relativo: Lo spazio vale di più se ne ho più degli altri
            results[i] += spaceCounts[i] * HeuristicsConstants.SpaceWeight;
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

    // FIX: Ora accetta snakeIndex per sapere chi escludere
    private float Head2HeadCollision(int snakeIndex, int myLength, ushort head)
    {
        for (var i = 0; i < _system.Count; i++) // Check all snakes
        {
            // Non controllo me stesso
            if (i == snakeIndex) continue; 
            
            var enemy = _system[i];
            
            if (enemy.IsDead || enemy.Length < myLength) continue;

            var enemyHead = enemy.Head;
            
            // Verifica collisione con le celle adiacenti alla mia testa (possibili mosse nemiche o posizione attuale)
            // Qui stiamo verificando se le mosse dalla mia 'head' portano alla 'enemyHead'.
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int Abs(int n)
    {
        var mask = n >> 31;
        return (n + mask) ^ mask;
    }
}