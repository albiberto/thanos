using System.Buffers;
using System.Numerics;
using System.Runtime.CompilerServices;
using Thanos.Common;
using Thanos.PreWarm.Memory;
using Thanos.SourceGen;
using Thanos.War.Grid;
using Thanos.War.Snake.Memory;

namespace Thanos.War;

public readonly ref struct Scout(WarGrid grid, WarSnakesMemoryView snakes, ReadOnlySpan<Coordinate> conversionsMap, ReadOnlySpan<double> positionalScores)
{
    private readonly WarGrid _grid = grid;
    private readonly WarSnakesMemoryView _snakes = snakes;
    private readonly ReadOnlySpan<Coordinate> _conversionsMap = conversionsMap;
    private readonly ReadOnlySpan<double> _positionalScores = positionalScores;

    // --- Pesi delle Euristiche Dinamiche ---
    private const double SpaceWeight = 75.0; // Punteggio per lo spazio VERO, calcolato ora
    private const double FoodWeight = 25.0; // Punteggio per il cibo

    // I pesi posizionali sono stati spostati nel builder della cache
    private const int SafeSpaceNodeBudget = 512;

    /// <summary>
    ///     SCEGLIE LA MOSSA PER IL ROLLOUT: Una policy veloce e cauta
    ///     per guidare le simulazioni, preferendo lo spazio futuro.
    /// </summary>
    public byte SelectRolloutMove(byte legalMoves)
    {
        if (legalMoves == 0) return Moves.Up;
        if (BitOperations.IsPow2(legalMoves)) return legalMoves;

        var bestMove = Moves.None;
        var bestScore = double.NegativeInfinity;

        var me = _snakes.Me;
        var head = me.Head;

        var movesToEvaluate = legalMoves;
        while (movesToEvaluate > 0)
        {
            var moveIndex = BitOperations.TrailingZeroCount(movesToEvaluate);
            var currentMove = (byte)(1 << moveIndex);

            var nextPos = _grid.GetNeighbor(head, currentMove);
            double currentMoveScore = 0;

            var futureMoves = _grid.GetLegalMoves(nextPos);
            currentMoveScore += 100 * BitOperations.PopCount(futureMoves);

            if (_grid.Food.IsSet(nextPos))
                currentMoveScore += 20;

            if (currentMoveScore > bestScore)
            {
                bestScore = currentMoveScore;
                bestMove = currentMove;
            }

            movesToEvaluate &= (byte)~currentMove;
        }

        return bestMove;
    }

    public double Evaluate()
    {
        var me = _snakes.Me;
        
        if (me.Dead) return double.NegativeInfinity;

        var head = me.Head;
        var health = me.Health;

        var food = _grid.Food.GetRawData;

        // Ottieni il "pacchetto" di LUT per la dimensione della griglia corrente
        var headCoord = _conversionsMap[head];

        var score = 0.0;

        // 1. PUNTEGGIO POSIZIONALE STATICO (dalla LUT)
        // Questo singolo lookup sostituisce Mobilità, Bordo e Centro.
        score += _positionalScores[head];

        // 2. INCENTIVO CIBO (dinamico)
        score += FoodWeight * CalculateFoodIncentive(headCoord, health, food, _conversionsMap);

        // 3. AREA SICURA (dinamico)
        // Questa è la valutazione più importante, perché tiene conto degli ostacoli ATTUALI.
        score += SpaceWeight * EstimateSafeSpaceBitset(head, _grid.Geography.Area, SafeSpaceNodeBudget, in _grid);

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

    private static int EstimateSafeSpaceBitset(ushort start, int area, int nodeBudget, in WarGrid grid)
    {
        if (area <= 0) return 0;
        var words = (area + 63) >> 6;
        ulong[]? rentedVisited = null;
        var visitedBits = words <= 16 ? stackalloc ulong[words] : (rentedVisited = ArrayPool<ulong>.Shared.Rent(words)).AsSpan(0, words);
        visitedBits.Clear();

        ushort[]? rentedQueue = null;
        var qCap = Math.Min(area, Math.Max(nodeBudget, 16));
        var queue = qCap <= 1024 ? stackalloc ushort[qCap] : (rentedQueue = ArrayPool<ushort>.Shared.Rent(qCap)).AsSpan(0, qCap);

        int qHead = 0, qTail = 0, count = 0, visitedCount = 0;

        static bool TryMarkVisited(Span<ulong> bits, int idx)
        {
            var word = idx >> 6;
            var m = 1UL << (idx & 63);
            if ((bits[word] & m) != 0) return false;
            bits[word] |= m;
            return true;
        }

        if (TryMarkVisited(visitedBits, start))
        {
            queue[qTail++] = start;
            visitedCount = 1;
        }

        while (qHead != qTail && count < nodeBudget)
        {
            var pos = queue[qHead];
            qHead = (qHead + 1) % qCap;
            count++;
            var moves = grid.GetLegalMoves(pos);
            while (moves != 0)
            {
                var moveIndex = BitOperations.TrailingZeroCount(moves);
                var currentMove = (byte)(1 << moveIndex);
                var next = grid.GetNeighbor(pos, currentMove);
                if (next != ushort.MaxValue && TryMarkVisited(visitedBits, next))
                {
                    visitedCount++;
                    if ((qTail + 1) % qCap != qHead)
                    {
                        queue[qTail] = next;
                        qTail = (qTail + 1) % qCap;
                    }
                }

                moves &= (byte)~currentMove;
            }
        }

        if (rentedVisited is not null) ArrayPool<ulong>.Shared.Return(rentedVisited);
        if (rentedQueue is not null) ArrayPool<ushort>.Shared.Return(rentedQueue);

        return visitedCount;
    }
}