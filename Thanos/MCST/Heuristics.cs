using System.Buffers;
using System.Numerics;
using Thanos.Common;
using Thanos.War;

namespace Thanos.MCST;

public static class Heuristics
{
    // --- Pesi delle Euristiche ---
    private const double MobilityWeight = 1.0;
    private const double FoodWeight = 150.0;
    private const double BorderPenalty = -30.0;
    private const double CenterBonus = 20.0;
    private const double SpaceWeight = 2.0;
    private const int SafeSpaceNodeBudget = 512;

    /// <summary>
    ///     SCEGLIE LA MOSSA MIGLIORE: valuta ogni mossa legale guardando un turno nel futuro
    ///     e scegliendo la mossa che porta alla posizione con il punteggio più alto.
    ///     Spezza i pareggi in modo casuale per evitare un comportamento deterministico.
    /// </summary>
    public static byte FindBestMove(byte legalMoves, ref WarArena arena)
    {
        if (legalMoves == 0) return Moves.Up;
        if (BitOperations.IsPow2(legalMoves)) return legalMoves;

        // --- INIZIO MODIFICA: Raccolta e Shuffle delle mosse ---
        Span<byte> moves = stackalloc byte[4];
        var count = 0;

        // Raccogli le mosse legali in uno Span
        var tempMoves = legalMoves;
        while (tempMoves > 0)
        {
            var moveIndex = BitOperations.TrailingZeroCount(tempMoves);
            var currentMove = (byte)(1 << moveIndex);
            moves[count++] = currentMove;
            tempMoves &= (byte)~currentMove;
        }

        // Mescola lo Span per rompere l'ordine deterministico
        Shuffle(moves[..count]);
        // --- FINE MODIFICA ---

        var bestMove = Moves.None;
        var bestScore = double.NegativeInfinity;

        // Ora itera sullo Span mescolato invece di usare il ciclo bitwise
        for (var i = 0; i < count; i++)
        {
            var currentMove = moves[i];

            // 1. Simula la mossa in una copia temporanea dell'arena
            var lookaheadArena = arena;
            lookaheadArena.ApplySingleMove(currentMove);

            // 2. Valuta la bontà della posizione risultante
            var currentMoveScore = Evaluate(ref lookaheadArena);

            if (currentMoveScore > bestScore)
            {
                bestScore = currentMoveScore;
                bestMove = currentMove;
            }
        }

        return bestMove != Moves.None ? bestMove : (byte)(1 << BitOperations.TrailingZeroCount(legalMoves));
    }

    /// <summary>
    ///     Mescola gli elementi di uno Span in modo casuale (algoritmo di Fisher-Yates).
    /// </summary>
    private static void Shuffle(Span<byte> span)
    {
        for (var i = span.Length - 1; i > 0; i--)
        {
            var j = Random.Shared.Next(i + 1);
            (span[i], span[j]) = (span[j], span[i]);
        }
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
        if (x == 0 || y == 0 || x == width - 1 || y == grid.Geography.Height - 1)
        {
            score += BorderPenalty;
        }
        else
        {
            var cx = width / 2;
            var cy = grid.Geography.Height / 2;
            var dCenter = Math.Abs(x - cx) + Math.Abs(y - cy);
            score += CenterBonus / (1 + dCenter);
        }

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

    // ===================================================
    // VERSIONE CORRETTA DI EstimateSafeSpaceBitset
    // ===================================================
    private static int EstimateSafeSpaceBitset(ushort start, ref WarArena arena, int nodeBudget)
    {
        // Allocazione e gestione memoria (invariata e corretta)
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

        // Helper per marcare i nodi visitati (invariato)
        static bool TryMarkVisited(Span<ulong> bits, int idx)
        {
            var word = idx >> 6;
            var m = 1UL << (idx & 63);
            if ((bits[word] & m) != 0) return false;
            bits[word] |= m;
            return true;
        }

        // Inizializza la coda
        if (TryMarkVisited(visitedBits, start))
        {
            queue[qTail++] = start;
            visitedCount = 1;
        }

        // BFS
        while (qHead != qTail && count < nodeBudget)
        {
            var pos = queue[qHead];
            qHead = (qHead + 1) % qCap;
            count++;

            var moves = arena.Grid.GetLegalMoves(pos);

            // ### INIZIO DELLA CORREZIONE ###
            while (moves != 0)
            {
                var moveIndex = BitOperations.TrailingZeroCount(moves);
                var currentMove = (byte)(1 << moveIndex);

                // Usa il metodo sicuro per ottenere il vicino, invece dell'aritmetica
                var next = arena.Grid.GetNeighbor(pos, currentMove);

                // Il tuo GetNeighbor ritorna ushort.MaxValue per i muri, quindi questo controllo
                // è implicito, ma per sicurezza lo aggiungiamo. IsOccupied già lo fa.
                if (next != ushort.MaxValue && TryMarkVisited(visitedBits, next))
                {
                    visitedCount++;
                    if ((qTail + 1) % qCap != qHead) // Controlla se la coda è piena
                    {
                        queue[qTail] = next;
                        qTail = (qTail + 1) % qCap;
                    }
                }

                moves &= (byte)~currentMove;
            }
            // ### FINE DELLA CORREZIONE ###
        }

        if (rentedVisited is not null) ArrayPool<ulong>.Shared.Return(rentedVisited);
        if (rentedQueue is not null) ArrayPool<ushort>.Shared.Return(rentedQueue);

        return visitedCount;
    }
}