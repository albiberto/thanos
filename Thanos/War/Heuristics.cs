using System.Numerics;
using System.Runtime.CompilerServices;
using Thanos.Common;
using Thanos.SourceGen;

namespace Thanos.War;

public static class HeuristicsConstants
{
    public const float SpaceWeight = 10.5f;

    public const float HealthWeight = 0.5f;
    public const float FoodWeight = 0.6f;

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

    public float Evaluate()
    {
        var me = _system.Me;
        if (me.IsDead) return float.NegativeInfinity;

        var head = me.Head;
        if (head >= _positionalScores.Length) return float.NegativeInfinity;

        var health = me.HP;
        var score = 0.0f;

        // --- PENALITÀ DINAMICA PER TRAPPOLE ---
        var openExits = 0;
        var currentWalls = _snakes;
        foreach (var move in AllMovesArray)
        {
            var neighbor = _neighborsGrid.Get(head, move);
            if (NeighborsGrid.IsValid(neighbor) && !currentWalls.IsSet(neighbor)) openExits++;
        }

        switch (openExits)
        {
            case <= 1:
                score -= 750.0f;
                break;
            case 2:
                score -= 200.0f;
                break;
        }

        // --- NUOVA EURISTICA: PREMIO PER LA SALUTE ---
        // Ricompensa direttamente lo stato di essere sani.
        score += health * HeuristicsConstants.HealthWeight;

        // --- EURISTICA DELLO SPAZIO (Flood Fill) ---
        var walls = _snakes;
        Span<byte> wallsMemoryCopy = stackalloc byte[walls.Raw.Length];
        walls.Raw.CopyTo(wallsMemoryCopy);
        var simulatedWalls = new Bitboard(wallsMemoryCopy);
        if (!me.WillGrow) simulatedWalls.Unset(me.Tail);
        var mySpace = FloodFill(head, simulatedWalls);
        score += mySpace * HeuristicsConstants.SpaceWeight;

        // // --- EURISTICA POSIZIONALE (Statica) ---
        // // QUESTA RIGA MANCAVA NEL TUO CODICE PRECEDENTE. È IMPORTANTE REINSERIRLA.
        // score += _positionalScores[head];
        //
        // // --- PENALITÀ DINAMICA PER TRAPPOLE ---
        // int openExits = 0;
        // var currentWalls = _grid.Snakes; 
        // foreach (var move in AllMovesArray)
        // {
        //     var neighbor = _grid.GetNeighbor(head, move);
        //     if (Grid.IsValid(neighbor) && !currentWalls.IsSet(neighbor)) openExits++;
        // }
        // if (openExits <= 1) score -= 750.0f; 
        // else if (openExits == 2) score -= 200.0f;

        // --- EURISTICA DEL CIBO ---
        var foodBitboard = _food.Memory;
        var headCoord = _conversionsMap[head];
        score += HeuristicsConstants.FoodWeight * CalculateFoodIncentive(headCoord, health, foodBitboard, _conversionsMap);

        return score;
    }

    /// <summary>
    ///     Calcola l'incentivo al cibo trovando la distanza minima in modo super-performante
    ///     usando la LUT per le coordinate.
    /// </summary>
    private static float CalculateFoodIncentive(Coordinate head, int health, ReadOnlySpan<ulong> food, ReadOnlySpan<Coordinate> map)
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

        EndLoop:
        if (distance is >= int.MaxValue or 0) return 0.0f;

        // --- NUOVA FORMULA PER L'URGENZA ---
        // L'urgenza ora è semplicemente l'inverso della salute.
        // A 100 HP, l'urgenza è 1. A 10 HP, l'urgenza è 91.
        // Scala in modo molto più naturale e prevedibile.
        // Usiamo 101 per evitare divisioni per zero se la salute fosse 101.
        var urgency = 101.0f - health;

        return urgency / distance;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int Abs(int n)
    {
        var mask = n >> 31;
        return (n + mask) ^ mask;
    }

    /// <summary>
    ///     Calcola il numero di caselle raggiungibili usando un algoritmo Flood Fill (Depth-First Search)
    ///     ottimizzato per non allocare memoria sul heap.
    /// </summary>
    /// <param name="startNode">La coordinata 1D da cui iniziare il riempimento.</param>
    /// <param name="walls">Una Bitboard che rappresenta tutti gli ostacoli.</param>
    /// <returns>Il numero di caselle accessibili.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [SkipLocalsInit]
    private int FloodFill(ushort startNode, in Bitboard walls)
    {
        // Uscita anticipata: se partiamo da un muro, lo spazio è zero.
        if (walls.IsSet(startNode)) return 0;

        // Definiamo una dimensione massima sicura per le strutture dati. 256 copre fino a 16x16.
        const int MaxStackSize = 256;
        const int BitboardMemorySize = MaxStackSize / 8; // 32 byte per 256 bit

        // Stack per l'algoritmo Depth-First Search.
        Span<ushort> stack = stackalloc ushort[MaxStackSize];

        // Memoria per la bitboard che tiene traccia delle caselle già visitate.
        Span<byte> visitedMemory = stackalloc byte[BitboardMemorySize];
        visitedMemory.Clear(); // FONDAMENTALE: azzera la memoria per evitare "false visite".

        // Creiamo la bitboard "visited" come una vista sulla memoria appena allocata.
        var visited = new Bitboard(visitedMemory);

        // --- Inizializzazione dell'algoritmo ---

        stack[0] = startNode;
        var stackPointer = 1; // Punta al prossimo slot libero

        visited.Set(startNode);
        var count = 1;

        // --- Ciclo principale della ricerca ---

        while (stackPointer > 0)
        {
            // "Pop" manuale del nodo corrente dallo stack.
            var current = stack[--stackPointer];

            // Itera sulle 4 possibili mosse invece di scrivere codice ripetuto.
            // Usa l'array statico già presente nella struct Heuristics.
            foreach (var move in AllMovesArray)
            {
                var neighbor = _neighborsGrid.Get(current, move);

                // Condizione unica e pulita per scartare un vicino:
                // - Se non è valido (fuori mappa, ushort.MaxValue)
                // - Se è un muro (un altro serpente)
                // - Se lo abbiamo già visitato
                if (!NeighborsGrid.IsValid(neighbor) || walls.IsSet(neighbor) || visited.IsSet(neighbor)) continue;

                // Se il vicino è valido, lo visitiamo e lo aggiungiamo allo stack.
                visited.Set(neighbor);
                stack[stackPointer++] = neighbor; // "Push"
                count++;
            }
        }

        return count;
    }
}