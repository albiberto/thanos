using System.Buffers;
using System.Numerics;
using Thanos.Common;
using Thanos.War;

namespace Thanos.MCST;

public static class Heuristics
{
    // --- Pesi delle Euristiche (Versione Ribilanciata per "Solo") ---
    private const double SpaceWeight = 75.0; // PRIORITÀ 1: Controllare più spazio possibile è la chiave per sopravvivere.
    private const double BorderPenalty = -200.0; // PRIORITÀ 2: I muri sono morte certa. La penalità deve essere forte e decisa.
    private const double FoodWeight = 25.0; // PRIORITÀ 3: Il cibo è importante, ma NON più dello spazio e dei muri.
    private const double CenterBonus = 5.0; // TIE-BREAKER: Un piccolo incentivo a rimanere al centro.
    private const double MobilityWeight = 1.0; // TIE-BREAKER: Un incentivo minimo a "muoversi" in spazi aperti.
    private const int SafeSpaceNodeBudget = 512;

    /// <summary>
    ///     SCEGLIE LA MOSSA PER IL ROLLOUT: Una policy veloce e cauta
    ///     per guidare le simulazioni, preferendo lo spazio futuro.
    /// </summary>
    public static byte SelectRolloutMove(byte legalMoves, ref WarArena arena)
    {
        if (legalMoves == 0) return Moves.Up;
        if (BitOperations.IsPow2(legalMoves)) return legalMoves;

        var bestMove = Moves.None;
        var bestScore = double.NegativeInfinity;

        var head = arena.Snakes.Me.Head;
        var grid = arena.Grid;

        var movesToEvaluate = legalMoves;
        while (movesToEvaluate > 0)
        {
            var moveIndex = BitOperations.TrailingZeroCount(movesToEvaluate);
            var currentMove = (byte)(1 << moveIndex);

            var nextPos = grid.GetNeighbor(head, currentMove);
            double currentMoveScore = 0;

            var futureMoves = grid.GetLegalMoves(nextPos);
            currentMoveScore += 100 * BitOperations.PopCount(futureMoves);

            if (grid.Food.IsSet(nextPos))
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

    /// <summary>
    ///     VALUTA UNA POSIZIONE: Assegna un punteggio a uno stato del gioco.
    /// </summary>
    public static double Evaluate(ref WarArena arena)
    {
        if (arena.Snakes.Me.Dead) return double.NegativeInfinity;

        var me = arena.Snakes.Me;
        var head = me.Head;
        var grid = arena.Grid;
        var width = grid.Geography.Width;

        var score = 0.0;

        // 1) Mobilità immediata
        score += MobilityWeight * BitOperations.PopCount(grid.GetLegalMoves(head));

        // 2) Incentivo cibo
        score += FoodWeight * CalculateFoodIncentive(ref arena);

        // 3) Penalità bordo e Bonus centro
        var x = head % width;
        var y = head / width;

        // <--- CORREZIONE: Applicati in modo indipendente ---
        if (x == 0 || y == 0 || x == width - 1 || y == grid.Geography.Height - 1) score += BorderPenalty;

        var cx = width / 2;
        var cy = grid.Geography.Height / 2;
        var dCenter = Math.Abs(x - cx) + Math.Abs(y - cy);
        score += CenterBonus / (1 + dCenter);
        // <--- FINE CORREZIONE ---

        // 4) Area sicura (stima flood-fill)
        score += SpaceWeight * EstimateSafeSpaceBitset(head, ref arena, SafeSpaceNodeBudget);

        return score;
    }

    // --- Euristiche di Supporto ---

    private static double CalculateFoodIncentive(ref WarArena arena)
    {
        var me = arena.Snakes.Me;
        var head = me.Head;
        var health = me.Health;

        var distance = FindClosestFoodDistance(head, arena.Grid.Food.GetRawData, arena.Grid.Geography.Width);
        if (distance is >= int.MaxValue or 0) return 0.0;

        var urgency = 100.0 - health + 30.0;
        return urgency / distance;
    }

    private static int FindClosestFoodDistance(ushort head, ReadOnlySpan<ulong> foodData, int w)
    {
        var headX = head % w;
        var headY = head / w;
        var minDistance = int.MaxValue;

        for (var i = 0; i < foodData.Length; i++)
        {
            var chunk = foodData[i];
            if (chunk == 0) continue;

            while (chunk != 0)
            {
                var bitIndex = BitOperations.TrailingZeroCount(chunk);
                var pos1D = (ushort)((i << 6) + bitIndex);
                var foodX = pos1D % w;
                var foodY = pos1D / w;
                var d = Math.Abs(headX - foodX) + Math.Abs(headY - foodY);
                if (d < minDistance) minDistance = d;
                chunk &= ~(1UL << bitIndex);
            }
        }

        return minDistance;
    }

    private static int EstimateSafeSpaceBitset(ushort start, ref WarArena arena, int nodeBudget)
    {
        // Questa funzione era già corretta e performante. Nessuna modifica.
        var cells = arena.Grid.Geography.Area;
        if (cells <= 0) return 0;
        var words = (cells + 63) >> 6;
        ulong[]? rentedVisited = null;
        var visitedBits = words <= 16 ? stackalloc ulong[words] : (rentedVisited = ArrayPool<ulong>.Shared.Rent(words)).AsSpan(0, words);
        visitedBits.Clear();

        ushort[]? rentedQueue = null;
        var qCap = Math.Min(cells, Math.Max(nodeBudget, 16));
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
            var moves = arena.Grid.GetLegalMoves(pos);
            while (moves != 0)
            {
                var moveIndex = BitOperations.TrailingZeroCount(moves);
                var currentMove = (byte)(1 << moveIndex);
                var next = arena.Grid.GetNeighbor(pos, currentMove);
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