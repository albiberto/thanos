using System;
using System.Collections.Generic;
using System.Numerics;
using Thanos.Common;
using Thanos.War;

namespace Thanos.MCST;

public static class Heuristics
{
    // --- PESI DELLE EURISTICHE ---
    // Sentiti libero di sperimentare con questi valori!
    private const float Alpha = 2.5f; // Peso per la mobilità
    private const float Beta = 1.0f;  // Peso per il controllo area (Flood Fill)
    private const float Delta = 7.0f; // Peso per la salute/vita
    private const float Epsilon = 5.5f; // Peso per la ricerca di cibo

    /// <summary>
    /// Sceglie la mossa migliore da un set di mosse legali,
    /// valutando lo stato del gioco dopo ogni possibile mossa.
    /// </summary>
    public static byte FindBestMove(byte legalMoves, ref WarArena arena)
    {
        if (BitOperations.PopCount(legalMoves) == 0) return Moves.Up;

        byte bestMove = Moves.None;
        float bestScore = float.MinValue;

        ReadOnlySpan<byte> allMoves = [Moves.Up, Moves.Down, Moves.Left, Moves.Right];

        foreach (var move in allMoves)
        {
            if ((legalMoves & move) == 0) continue;

            var futureArena = arena;
            futureArena.ApplySingleMove(move);

            float currentScore;
            if (futureArena.Snakes.Me.Dead)
            {
                currentScore = float.MinValue; // La morte ha sempre il punteggio peggiore
            }
            else
            {
                currentScore = Evaluate(ref futureArena);
            }

            if (currentScore > bestScore)
            {
                bestScore = currentScore;
                bestMove = move;
            }
        }
        
        return bestMove != Moves.None ? bestMove : (byte)(1 << BitOperations.TrailingZeroCount(legalMoves));
    }

    /// <summary>
    /// Calcola un punteggio numerico per una data configurazione del gioco.
    /// </summary>
    public static float Evaluate(ref WarArena arena)
    {
        var floodFill = CalculateFloodFillArea(ref arena);
        var health = CalculateHealth(ref arena);
        var foodIncentive = CalculateFoodIncentive(ref arena);
        var mobility = CalculateMobilityImmediate(ref arena);
        
        // La Voronoi è intenzionalmente lasciata a 0 come da discussione precedente
        var voronoi = 0f;

        var score = (Alpha * mobility)
                  + (Beta * floodFill)
                  + (Delta * health)
                  + (Epsilon * foodIncentive);

        return score;
    }

    // --- IMPLEMENTAZIONE DELLE SINGOLE EURISTICHE ---

    private static float CalculateMobilityImmediate(ref WarArena arena)
    {
        var legalMoves = arena.Grid.GetLegalMoves(arena.Snakes.Me.Head);
        return BitOperations.PopCount(legalMoves);
    }

    private static float CalculateHealth(ref WarArena arena) => arena.Snakes.Me.Health / 10.0f;

    /// <summary>
    /// Calcola la dimensione dell'area sicura partendo dalla testa del serpente.
    /// </summary>
    private static float CalculateFloodFillArea(ref WarArena arena)
    {
        return CalculateFloodFillArea(ref arena, arena.Snakes.Me.Head);
    }
    
    /// <summary>
    /// **NUOVA VERSIONE FLESSIBILE**
    /// Calcola la dimensione dell'area sicura partendo da una cella specifica.
    /// </summary>
    private static float CalculateFloodFillArea(ref WarArena arena, ushort startCell)
    {
        var grid = arena.Grid;
        var queue = new Queue<ushort>();
        var visited = new HashSet<int>();

        if (!grid.IsOccupied(startCell))
        {
            queue.Enqueue((ushort)startCell);
            visited.Add(startCell);
        }
        else
        {
            return 0; // Il punto di partenza non è valido
        }

        int areaSize = 0;
        ReadOnlySpan<byte> allMoves = [Moves.Up, Moves.Down, Moves.Left, Moves.Right];

        while (queue.Count > 0)
        {
            var currentCell = queue.Dequeue();
            areaSize++;

            foreach (var move in allMoves)
            {
                var neighbor = grid.GetNeighbor(currentCell, move);
                if (!visited.Contains(neighbor) && !grid.IsOccupied(neighbor))
                {
                    visited.Add(neighbor);
                    queue.Enqueue(neighbor);
                }
            }
        }
        return areaSize;
    }

    /// <summary>
    /// **LOGICA DI URGENZA MIGLIORATA**
    /// Calcola un punteggio che incoraggia il serpente a mangiare quando ne ha bisogno.
    /// </summary>
    private static float CalculateFoodIncentive(ref WarArena arena)
    {
        var mySnake = arena.Snakes.Me;

        // Inizia a considerare il cibo quando la vita scende sotto 80
        if (mySnake.Health > 80)
        {
            return 0f;
        }
        
        FindClosestSafeFood(ref arena, out int distanceToFood);
        
        if (distanceToFood <= 0) // Nessun cibo sicuro trovato o siamo già sul cibo
        {
            return 0f;
        }

        // Urgenza quadratica per un effetto "panico" a bassa vita
        float proximityBonus = 1.0f / distanceToFood;
        float healthDeficit = 100f - mySnake.Health;
        float urgencyBonus = (healthDeficit * healthDeficit) / 10f;

        return proximityBonus * urgencyBonus;
    }

    /// <summary>
    /// **CONTROLLO DI SICUREZZA CORRETTO**
    /// Trova il cibo più vicino e sicuro usando un BFS.
    /// </summary>
    private static void FindClosestSafeFood(ref WarArena arena, out int distance)
    {
        var grid = arena.Grid;
        var mySnake = arena.Snakes.Me;
        var myHead = mySnake.Head;
        
        var queue = new Queue<(ushort cell, int dist)>();
        var visited = new HashSet<int> { myHead };

        queue.Enqueue((myHead, 0));

        while (queue.Count > 0)
        {
            var (currentCell, currentDist) = queue.Dequeue();

            if (grid.IsFood(currentCell) && currentDist > 0)
            {
                // **FIX APPLICATO QUI**
                // Ora calcoliamo l'area partendo dalla posizione del CIBO.
                var areaAroundFood = CalculateFloodFillArea(ref arena, currentCell);

                if (areaAroundFood < mySnake.Length + 2) // +2 per un margine di sicurezza extra
                {
                    continue; // Cibo-trappola, ignora e continua la ricerca
                }

                distance = currentDist;
                return;
            }
            
            ReadOnlySpan<byte> allMoves = [Moves.Up, Moves.Down, Moves.Left, Moves.Right];
            foreach (var move in allMoves)
            {
                var neighbor = grid.GetNeighbor(currentCell, move);
                if (!visited.Contains(neighbor) && !grid.IsOccupied(neighbor))
                {
                    visited.Add(neighbor);
                    queue.Enqueue((neighbor, currentDist + 1));
                }
            }
        }
        
        distance = -1; // Nessun cibo sicuro trovato
    }
}