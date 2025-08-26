using System.Numerics;
using Thanos.Common;
using Thanos.War;
using Thanos.War.Grid;

namespace Thanos.MCST;

public static class Heuristics
{
    public static byte FindBestMove(byte legalMoves, ref WarArena arena)
    {
        if (legalMoves == 0) return Moves.Up;

        // Se c'è una sola mossa, non c'è bisogno di valutare nulla.
        if (BitOperations.IsPow2(legalMoves)) return legalMoves;

        var bestMove = Moves.None;
        var bestScore = double.NegativeInfinity;

        var movesToEvaluate = legalMoves;
        while (movesToEvaluate > 0)
        {
            var moveIndex = BitOperations.TrailingZeroCount(movesToEvaluate);
            var currentMove = (byte)(1 << moveIndex);

            // --- LOGICA CHIAVE ---
            // 1. Crea una copia dell'arena per simulare la mossa.
            var lookaheadArena = arena;

            // 2. Applica la mossa alla copia.
            lookaheadArena.ApplySingleMove(currentMove);

            // 3. Usa la nostra nuova funzione Evaluate per ottenere il punteggio della posizione risultante.
            var currentMoveScore = Evaluate(ref lookaheadArena);
            // --- FINE LOGICA CHIAVE ---

            if (currentMoveScore > bestScore)
            {
                bestScore = currentMoveScore;
                bestMove = currentMove;
            }

            movesToEvaluate &= (byte)~currentMove;
        }

        return bestMove != Moves.None ? bestMove : (byte)(1 << BitOperations.TrailingZeroCount(legalMoves));
    }

    /// <summary>
    ///     Valuta la bontà di una data posizione sulla scacchiera.
    ///     Un punteggio alto indica una posizione vantaggiosa.
    /// </summary>
    public static double Evaluate(ref WarArena arena)
    {
        const double mobilityWeight = 15.0;
        const double foodWeight = 1.5;

        // Se il nostro serpente è morto in questa posizione, è la peggiore possibile.
        if (arena.Snakes.Me.Dead) return double.NegativeInfinity;

        // Calcola il punteggio combinando le varie euristiche con i loro pesi.
        var finalScore = 0.0;

        // Ottieni la posizione attuale della testa per le euristiche
        var me = arena.Snakes.Me;
        var head = me.Head;
        var health = me.Health;

        // --- Euristica 1: Mobilità Immediata (Avere più opzioni è sempre meglio)---
        finalScore += mobilityWeight * CalculateImmediateMobilityScore(head, ref arena);

        // --- Euristica 2: Incentivo al Cibo ---
        finalScore += foodWeight * CalculateFoodIncentive(head, health, ref arena);


        return finalScore;
    }

    private static double CalculateImmediateMobilityScore(ushort headPosition, ref WarArena arena)
    {
        var legalMoves = arena.Grid.GetLegalMoves(headPosition);
        return BitOperations.PopCount(legalMoves);
    }

    /// <summary>
    ///     Calcola un punteggio che incentiva il serpente a cercare cibo.
    ///     L'incentivo è più forte se la vita è bassa.
    /// </summary>
    private static double CalculateFoodIncentive(ushort head, int health, ref WarArena arena)
    {
        var distance = FindClosestFoodDistance(head, ref arena);

        // Se non c'è cibo o siamo già sul cibo, non c'è incentivo
        if (distance is >= int.MaxValue or 0) return 0.0;

        // L'"urgenza" di mangiare è inversamente proporzionale alla vita.
        // Se la vita è 100, l'urgenza è 0. Se la vita è 10, l'urgenza è 90.
        var urgency = 100.0 - health;

        // Il punteggio combina urgenza e vicinanza (più sei vicino, più è alto).
        return urgency / distance;
    }

    /// <summary>
    ///     FUNZIONE DI SUPPORTO: Trova la distanza di Manhattan dal cibo più vicino.
    ///     Scansiona il bitboard del cibo in modo efficiente.
    ///     Ritorna int.MaxValue se non c'è cibo.
    /// </summary>
    private static int FindClosestFoodDistance(ushort headPosition, ref WarArena arena)
    {
        var minDistance = int.MaxValue;

        // Converti la posizione della testa in 2D una sola volta
        var headX = headPosition % arena.Grid.Geography.Width;
        var headY = headPosition / arena.Grid.Geography.Width;

        // Ottieni i dati grezzi del bitboard del cibo
        var foodData = arena.Grid.Food.GetRawData();

        // Itera sui blocchi di 64 bit del bitboard
        for (var i = 0; i < foodData.Length; i++)
        {
            var chunk = foodData[i];
            if (chunk == 0) continue; // Salta i blocchi senza cibo

            // Finché ci sono bit accesi in questo blocco, trovali
            while (chunk != 0)
            {
                var bitIndex = BitOperations.TrailingZeroCount(chunk);
                var foodPosition1D = (ushort)(i * 64 + bitIndex);

                // Calcola la distanza di Manhattan
                var foodX = foodPosition1D % arena.Grid.Geography.Width;
                var foodY = foodPosition1D / arena.Grid.Geography.Width;
                var distance = Math.Abs(headX - foodX) + Math.Abs(headY - foodY);

                if (distance < minDistance) minDistance = distance;

                // Spegni il bit che abbiamo appena processato per passare al prossimo
                chunk &= ~(1UL << bitIndex);
            }
        }

        return minDistance;
    }
}