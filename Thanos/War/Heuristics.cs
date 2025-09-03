using System.Numerics;
using System.Runtime.CompilerServices;
using Thanos.Common;
using Thanos.SourceGen;
using Thanos.War.Grid;
using Thanos.War.Snake;

namespace Thanos.War;

public readonly ref struct Heuristics
{
    // --- COSTANTI E PESI DELL'EURISTICA (Tutto in un unico posto) ---
    
    /// <summary>
    /// La penalità per trovarsi su una casella del bordo.
    /// Deve essere un valore negativo forte per scoraggiare il serpente.
    /// </summary>
    public const double BorderPenaltyValue = -100.0;

    /// <summary>
    /// Il bonus massimo per trovarsi al centro esatto del tabellone.
    /// Il bonus diminuisce allontanandosi dal centro.
    /// </summary>
    public const double CenterBonusValue = 25.0;
    
    /// <summary>
    /// Controlla l'importanza di avere più spazio a disposizione.
    /// È il fattore più importante per la sopravvivenza.
    /// </summary>
    private const double SpaceWeight = 3.0;

    /// <summary>
    /// Controlla l'importanza del cibo. Viene usato solo quando la salute è bassa.
    /// </summary>
    private const double FoodWeight = 0.5;

    /// <summary>
    /// La soglia di salute sotto la quale il serpente inizia a cercare attivamente cibo.
    /// </summary>
    private const int HealthThreshold = 40;

    // --- Campi e Costruttore ---

    private readonly WarGrid Grid;
    private readonly WarSnake Me;
    private readonly Enemies Enemies;
    private readonly ReadOnlySpan<Coordinate> _conversionsMap;
    private readonly ReadOnlySpan<double> _positionalScores;
    private static readonly byte[] AllMovesArray = [Moves.Up, Moves.Down, Moves.Left, Moves.Right];


    public Heuristics(WarGrid grid, WarSnake me, Enemies enemies, ReadOnlySpan<Coordinate> conversionsMap, ReadOnlySpan<double> positionalScores)
    {
        Grid = grid;
        Me = me;
        Enemies = enemies;
        _conversionsMap = conversionsMap;
        _positionalScores = positionalScores;
    }

    public double Evaluate()
    {
        // 1. Condizione Terminale: Se siamo morti, questo è lo scenario peggiore in assoluto.
        if (Me.Dead) return double.NegativeInfinity;

        var head = Me.Head;
        var health = Me.Health;
        var score = 0.0;
        
        // --- 1. EURISTICA DELLO SPAZIO (Flood Fill) ---
        var walls = Grid.Snakes;
        if (!Me.WillGrow) // Assumendo che WarSnake esponga questa informazione
        {
            walls.Unset(Me.Tail);
        }
        var mySpace = FloodFill(head, walls);
        score += SpaceWeight * mySpace;

        // --- 2. EURISTICA POSIZIONALE (Statica) ---
        score += _positionalScores[head];

        // --- 3. EURISTICA DEL CIBO (CONDIZIONALE) ---
        // Cerca il cibo SOLO se la vita è sotto la soglia.
        if (health >= HealthThreshold) return score;
        
        var food = Grid.Food.GetRawData;
        var headCoord = _conversionsMap[head];
        score += FoodWeight * CalculateFoodIncentive(headCoord, health, food, _conversionsMap);

        return score;
    }

// --- Euristiche di Supporto ---
/// <summary>
///     Calcola l'incentivo al cibo trovando la distanza minima in modo super-performante
///     usando la LUT per le coordinate.
/// </summary>
private static double CalculateFoodIncentive(Coordinate head, int health, ReadOnlySpan<ulong> food, ReadOnlySpan<Coordinate> map)
    {
        var distance = int.MaxValue;

        for (var i = 0; i < food.Length; i++)
        {
            var chunk = food[i];
            if (chunk == 0) continue;

            while (chunk != 0)
            {
                var bitIndex = BitOperations.TrailingZeroCount(chunk);
                var pos1D = (ushort)((i << 6) + bitIndex);

                var foodCoords = map[pos1D];

                var d = Abs(head.X - foodCoords.X) + Abs(head.Y - foodCoords.Y);
                if (d < distance)
                {
                    distance = d;
                    if (distance == 1) goto EndLoop;
                }

                chunk &= ~(1UL << bitIndex);
            }
        }

        EndLoop: // Etichetta per uscire da entrambi i cicli
        if (distance is >= int.MaxValue or 0) return 0.0;

        var urgency = 100.0 - health + 30.0;
        return urgency / distance;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int Abs(int n)
    {
        // Maschera con tutti i bit a 1 se n è negativo, 0 se positivo
        var mask = n >> 31;
        // (n XOR mask) - mask
        return (n + mask) ^ mask;
    }
    
    /// <summary>
/// Calcola il numero di caselle raggiungibili usando un algoritmo Flood Fill (Depth-First Search)
/// ottimizzato per non allocare memoria sul heap.
/// </summary>
/// <param name="startNode">La coordinata 1D da cui iniziare il riempimento.</param>
/// <param name="walls">Una Bitboard che rappresenta tutti gli ostacoli.</param>
/// <returns>Il numero di caselle accessibili.</returns>
[MethodImpl(MethodImplOptions.AggressiveInlining)]
[SkipLocalsInit] // Ottimizzazione: dice al compilatore di non inizializzare a zero lo stack
private int FloodFill(ushort startNode, Bitboard walls)
{
    if (walls.IsSet(startNode)) return 0;

    // 1. Usiamo un array allocato sullo stack invece di una Queue sul heap.
    //    La dimensione 256 è più che sufficiente per qualsiasi area contigua in Battlesnake.
    Span<ushort> stack = stackalloc ushort[256];
    
    var visited = new Bitboard();
    var count = 0;
    var stackPointer = 0;

    // Inizializza lo stack con il nodo di partenza
    stack[stackPointer++] = startNode;
    visited.Set(startNode);
    count++;

    // 2. Il ciclo continua finché ci sono nodi da visitare nello stack.
    while (stackPointer > 0)
    {
        // "Pop" manuale dallo stack: più veloce di una chiamata a metodo.
        var current = stack[--stackPointer];

        // Esamina i 4 vicini
        // NOTA: Per la massima performance, potresti avere un metodo in Grid
        // che restituisce uno Span<ushort> di vicini per evitare di chiamare GetNeighbor 4 volte.
        // Ma anche così è già molto veloce.
        
        var neighborUp = Grid.GetNeighbor(current, Moves.Up);
        if (neighborUp != ushort.MaxValue && !walls.IsSet(neighborUp) && !visited.IsSet(neighborUp))
        {
            visited.Set(neighborUp);
            count++;
            stack[stackPointer++] = neighborUp; // "Push" manuale
        }
        
        var neighborDown = Grid.GetNeighbor(current, Moves.Down);
        if (neighborDown != ushort.MaxValue && !walls.IsSet(neighborDown) && !visited.IsSet(neighborDown))
        {
            visited.Set(neighborDown);
            count++;
            stack[stackPointer++] = neighborDown;
        }

        var neighborLeft = Grid.GetNeighbor(current, Moves.Left);
        if (neighborLeft != ushort.MaxValue && !walls.IsSet(neighborLeft) && !visited.IsSet(neighborLeft))
        {
            visited.Set(neighborLeft);
            count++;
            stack[stackPointer++] = neighborLeft;
        }

        var neighborRight = Grid.GetNeighbor(current, Moves.Right);
        if (neighborRight != ushort.MaxValue && !walls.IsSet(neighborRight) && !visited.IsSet(neighborRight))
        {
            visited.Set(neighborRight);
            count++;
            stack[stackPointer++] = neighborRight;
        }
    }
    
    return count;
}
}