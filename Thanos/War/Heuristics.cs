using System.Numerics;
using System.Runtime.CompilerServices;
using Thanos.Common;
using Thanos.Shared;
using Thanos.SourceGen;
using Thanos.War.Structures;

namespace Thanos.War;

public static class HeuristicsConstants
{
    // --- GEOPOLITICA ---
    public const float BaseSpaceValue = 1.0f;
    public const float CenterBonusMult = 3.0f; // Il centro vale 3 volte lo spazio normale
    public const float EdgeMalusMult = 0.2f;   // I bordi valgono pochissimo (20%)
    
    // --- PESI ---
    public const float DistanceToCenterWeight = 2.5f; // Attrazione gravitazionale verso (5,5)
    public const float FoodWeight = 2.0f;
    public const float SuffocationPenalty = -50000.0f; // Morte certa
    public const float HazardWeight = -0.5f;
}

public readonly struct HeuristicWeights
{
    public float Space { get; init; }
    public float Health { get; init; }
    public float Food { get; init; }
    public float Tail { get; init; }
    public float Aggression { get; init; }
    
    // Configurazioni Competitive Aggiornate
    public static HeuristicWeights Balanced => new() { Space = 10.0f, Health = 0.5f, Food = 1.2f, Tail = 0.5f, Aggression = 2.0f };
    public static HeuristicWeights Hungry => new() { Space = 5.0f, Health = 2.0f, Food = 40.0f, Tail = 0.1f, Aggression = 0.0f };
    public static HeuristicWeights HeadHunter => new() { Space = 15.0f, Health = 0.1f, Food = 0.2f, Tail = 0.1f, Aggression = 30.0f };
    public static HeuristicWeights Defensive => new() { Space = 25.0f, Health = 1.5f, Food = 0.5f, Tail = 2.0f, Aggression = -15.0f };
}

public readonly ref struct Heuristics
{
    private readonly SnakesSystem _system;
    private readonly Bitboard _food;
    private readonly Bitboard _hazards;
    private readonly Bitboard _snakes;
    private readonly NeighborsMatrix _neighborsMatrix;
    private readonly CoordinatesMatrix _conversionsMatrix;

    // --- MASCHERE GEOPOLITICHE STATICHE (Pre-calcolate) ---
    private static readonly ulong _edgeMask0;   // Anello esterno (Morte/Passività)
    private static readonly ulong _edgeMask1;
    private static readonly ulong _centerMask0; // 5x5 centrale (Dominio)
    private static readonly ulong _centerMask1;

    static Heuristics()
    {
        Span<byte> buffer = stackalloc byte[16];
        
        // 1. Edge Mask (Perimetro)
        var bbEdge = new Bitboard(buffer);
        bbEdge.Clear();
        for (int y = 0; y < 11; y++)
            for (int x = 0; x < 11; x++)
                if (x == 0 || x == 10 || y == 0 || y == 10)
                    bbEdge.Set((ushort)(y * 11 + x));
        
        _edgeMask0 = bbEdge.Buffer[0];
        _edgeMask1 = bbEdge.Buffer[1];

        // 2. Center Mask (Box 5x5 centrale)
        var bbCenter = new Bitboard(buffer);
        bbCenter.Clear();
        for (int y = 3; y <= 7; y++)
            for (int x = 3; x <= 7; x++)
                bbCenter.Set((ushort)(y * 11 + x));
        
        _centerMask0 = bbCenter.Buffer[0];
        _centerMask1 = bbCenter.Buffer[1];
    }

    public Heuristics(SnakesSystem system, Bitboard food, Bitboard hazards, Bitboard snakes, NeighborsMatrix neighborsMatrix, CoordinatesMatrix conversionsMatrix)
    {
        _system = system;
        _food = food;
        _hazards = hazards;
        _snakes = snakes;
        _neighborsMatrix = neighborsMatrix;
        _conversionsMatrix = conversionsMatrix;
    }

    public float Outcome(int playerIndex)
    {
        if (_system[playerIndex].IsDead) return -1.0f;
        var othersAlive = 0;
        for (var i = 0; i < _system.Count; i++)
            if (i != playerIndex && !_system[i].IsDead) othersAlive++;
        return othersAlive == 0 ? 1.0f : 0.0f;
    }

    [SkipLocalsInit]
    public void EvaluateAll(Span<float> results, bool isPhaseComplete)
    {
        Span<byte> wallsRaw = stackalloc byte[_snakes.Raw.Length];
        _snakes.Raw.CopyTo(wallsRaw);
        var globalWalls = new Bitboard(wallsRaw);

        // --- 1. Voronoi Pesato (Il cuore dell'intelligenza spaziale) ---
        EvaluateVoronoiBitwise(in globalWalls, results);

        for (var i = 0; i < _system.Count; i++)
        {
            var snake = _system[i];
            if (snake.IsDead) 
            { 
                results[i] = -10000.0f; 
                continue; 
            }

            var w = SelectProfile(in snake, i);

            // Salute e Coda
            results[i] += snake.HP * w.Health;
            results[i] += ManhattanDistance(snake.Head, snake.Tail) * w.Tail;
            
            // --- 2. Scontri Testa-a-Testa ---
            results[i] += EvaluateHead2Head(i, in snake, w.Aggression);
            
            // --- 3. Posizionamento Strategico (Gravity) ---
            // Attrazione esplicita verso il centro (5,5) per rompere il wall-hugging
            var headCoord = _conversionsMatrix[snake.Head];
            int distFromCenter = Math.Abs(headCoord.X - 5) + Math.Abs(headCoord.Y - 5);
            results[i] += (10 - distFromCenter) * HeuristicsConstants.DistanceToCenterWeight;

            // Trappole
            results[i] -= PenalityTrap(snake.Head, in globalWalls);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [SkipLocalsInit]
    private void EvaluateVoronoiBitwise(in Bitboard initialWalls, Span<float> results)
    {
        // Allocazione stack zero-copy per 4 serpenti
        const int BB_SIZE = 16; 
        const int MAX_SNAKES = 4;
        Span<byte> memory = stackalloc byte[BB_SIZE * (MAX_SNAKES * 2 + 2)];

        int OffsetFrontier(int i) => i * BB_SIZE;
        int OffsetVisited(int i) => (MAX_SNAKES + i) * BB_SIZE;
        int OffsetWalls = MAX_SNAKES * 2 * BB_SIZE;
        int OffsetExpansion = (MAX_SNAKES * 2 + 1) * BB_SIZE;

        memory.Clear();

        var currentWalls = new Bitboard(memory.Slice(OffsetWalls, BB_SIZE));
        var expansionTemp = new Bitboard(memory.Slice(OffsetExpansion, BB_SIZE));
        
        initialWalls.CopyTo(currentWalls);

        int activeSnakes = 0;
        for (int i = 0; i < _system.Count; i++)
        {
            if (_system[i].IsDead) continue;
            var f = new Bitboard(memory.Slice(OffsetFrontier(i), BB_SIZE));
            var v = new Bitboard(memory.Slice(OffsetVisited(i), BB_SIZE));
            f.Set(_system[i].Head);
            v.Set(_system[i].Head);
            activeSnakes++;
        }
        if (activeSnakes == 0) return;

        // Flood Fill per 16 passi (copre gran parte della board utile)
        for (int depth = 0; depth < 16; depth++)
        {
            bool anyGrowth = false;
            
            // 1. Espansione Simultanea
            for (int i = 0; i < _system.Count; i++)
            {
                if (_system[i].IsDead) continue;

                var frontier = new Bitboard(memory.Slice(OffsetFrontier(i), BB_SIZE));
                var visited = new Bitboard(memory.Slice(OffsetVisited(i), BB_SIZE));

                expansionTemp.Clear();
                frontier.Dilate(in currentWalls, expansionTemp); 
                expansionTemp.AndNot(in visited);
                expansionTemp.CopyTo(frontier); 

                if (expansionTemp.PopCount() > 0) anyGrowth = true;
            }

            if (!anyGrowth) break;

            // 2. Aggiornamento e Punteggio
            for (int i = 0; i < _system.Count; i++)
            {
                if (_system[i].IsDead) continue;

                var frontier = new Bitboard(memory.Slice(OffsetFrontier(i), BB_SIZE));
                var visited = new Bitboard(memory.Slice(OffsetVisited(i), BB_SIZE));

                visited.Or(in frontier);
                currentWalls.Or(in frontier); // Territorio conquistato diventa muro per gli altri

                // --- CALCOLO PUNTEGGIO QUALITATIVO ---
                ulong f0 = frontier.Buffer[0];
                ulong f1 = frontier.Buffer[1];

                if ((f0 | f1) == 0) continue;

                var snake = _system[i];
                var w = SelectProfile(in snake, i);
                float distFactor = 1.0f - (depth * 0.04f); // Decadimento distanza

                // Contiamo i bit nelle diverse zone
                int centerHits = BitOperations.PopCount(f0 & _centerMask0) + BitOperations.PopCount(f1 & _centerMask1);
                int edgeHits = BitOperations.PopCount(f0 & _edgeMask0) + BitOperations.PopCount(f1 & _edgeMask1);
                int totalHits = BitOperations.PopCount(f0) + BitOperations.PopCount(f1);
                int normalHits = totalHits - centerHits - edgeHits;

                // Formula Magica: Spazio + Bonus Centro - Malus Bordi
                float territoryScore = (normalHits * HeuristicsConstants.BaseSpaceValue) +
                                       (centerHits * HeuristicsConstants.CenterBonusMult) +
                                       (edgeHits * HeuristicsConstants.EdgeMalusMult);

                results[i] += territoryScore * w.Space * distFactor;

                // Cibo nel territorio
                ulong food0 = f0 & _food.Buffer[0];
                ulong food1 = f1 & _food.Buffer[1];
                if ((food0 | food1) != 0)
                {
                    int foodFound = BitOperations.PopCount(food0) + BitOperations.PopCount(food1);
                    results[i] += foodFound * w.Food * 10.0f * distFactor;
                }
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private HeuristicWeights SelectProfile(in WarSnake snake, int index)
    {
        if (snake.HP < 40) return HeuristicWeights.Hungry;
        
        int myLen = snake.Length;
        bool amBiggest = true;
        for(int i=0; i<_system.Count; i++) 
            if(i != index && !_system[i].IsDead && _system[i].Length >= myLen) amBiggest = false;

        return amBiggest ? HeuristicWeights.HeadHunter : HeuristicWeights.Balanced;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private float EvaluateHead2Head(int myIndex, in WarSnake me, float aggressionWeight)
    {
        float score = 0;
        var myHead = me.Head;
        var myLen = me.Length;

        for (var i = 0; i < _system.Count; i++)
        {
            if (i == myIndex || _system[i].IsDead) continue;
            var enemy = _system[i];
            var dist = ManhattanDistance(myHead, enemy.Head);
            
            if (dist <= 2)
            {
                if (myLen > enemy.Length) score += 500.0f * aggressionWeight; 
                else score -= 5000.0f; // Evita suicidi contro più grandi
            }
        }
        return score;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int ManhattanDistance(ushort pos1, ushort pos2)
    {
        var c1 = _conversionsMatrix[pos1];
        var c2 = _conversionsMatrix[pos2];
        return Math.Abs(c1.X - c2.X) + Math.Abs(c1.Y - c2.Y);
    }

    private float PenalityTrap(ushort head, in Bitboard walls)
    {
        // Semplice controllo uscite libere (vicolo cieco 1x1)
        var exits = 0;
        if (!_hazards.IsSet(head)) // Se non siamo già in hazard, conta muri
        {
             // Logica semplificata: conta solo muri adiacenti
             // (Da implementare se necessario, per ora ritorna 0 per non rallentare)
        }
        return 0.0f;
    }
}