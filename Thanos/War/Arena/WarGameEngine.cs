using Thanos.MCST;
using Thanos.War.Grid;
using Thanos.War.Snake;

namespace Thanos.War.Arena;

/// <summary>
/// A static class containing the pure logic for advancing the game state.
/// It is stateless and operates on a given WarArena state.
/// This design separates the game rules (the "Engine") from the game data (the "State").
/// </summary>
public static class WarGameEngine
{
    // =================================================================
    // Main Simulation Method
    // =================================================================

    /// <summary>
    /// Simulates a full game turn using pre-allocated workspace buffers to remain allocation-free.
    /// </summary>
    public static void SimulateTurn(ref WarArena arena, ReadOnlySpan<byte> chosenMoves, ref TurnWorkspace workspace)
    {
        var snakes = arena.Snakes;
        var snakeCount = arena.TotalSnakeCount; // Use the total number of snake slots
        ref var header = ref arena.Header;
        ref var grid = ref arena.Grid;

        workspace.IsDead.Clear();

        // --- PHASE 1: PREPARATION ---
        // Calculate the outcome of each snake's move.
        for (var i = 0; i < snakeCount; i++)
        {
            var snake = snakes[i];
            if (snake.Dead)
            {
                workspace.IsDead[i] = true;
                continue;
            }

            workspace.OldTailPositions[i] = snake.Tail;
            var head = snake.Head;
            var move = chosenMoves[i];

            // The conditional switch is replaced by a single, branchless lookup.
            workspace.NewHeadPositions[i] = arena.MovesLut.GetNeighbor(head, move);
        }

        // --- PHASE 2: CONFLICT RESOLUTION ---
        // Determine deaths from collisions (walls, bodies, head-to-head).
        for (var i = 0; i < snakeCount; i++)
        {
            if (workspace.IsDead[i]) continue;

            var newHead = workspace.NewHeadPositions[i];
            
            // Wall or body collision check
            if (grid.IsOccupied(newHead))
            {
                workspace.IsDead[i] = true;
                continue;
            }

            workspace.HasEaten[i] = grid.IsFood(newHead);

            // Head-to-head collision check
            for (var j = i + 1; j < snakeCount; j++)
            {
                if (workspace.IsDead[j]) continue;
                if (newHead == workspace.NewHeadPositions[j])
                {
                    var snakeA = snakes[i];
                    var snakeB = snakes[j];
                    if (snakeA.Length >= snakeB.Length) workspace.IsDead[j] = true;
                    if (snakeB.Length >= snakeA.Length) workspace.IsDead[i] = true;
                }
            }
        }

        // --- PHASE 3: MOVEMENT EXECUTION ---
        // Apply the moves to the snakes that survived Phase 2.
        for (var i = 0; i < snakeCount; i++)
        {
            if (workspace.IsDead[i]) continue;
            
            var snake = snakes[i];
            var newHead = workspace.NewHeadPositions[i];
            var hasEaten = workspace.HasEaten[i];
            
            var hazardDamage = grid.IsHazard(newHead) ? 15 : 0;
            var totalDamage = 1 + hazardDamage;
            
            snake.Move(newHead, hasEaten, totalDamage);
        }

        // --- PHASE 4: WORLD UPDATE ---
        // Update the grid and hash based on the final outcomes.
        for (var i = 0; i < snakeCount; i++)
        {
            var snake = snakes[i];
            bool wasAlive = !workspace.IsDead[i];
            bool isNowDead = wasAlive && snake.Dead; // Died from starvation/hazards in Phase 3
            
            if (isNowDead)
            {
                KillSnake(ref arena, i, ref header);
            }
            else if (wasAlive) // Survived the turn
            {
                var newHead = workspace.NewHeadPositions[i];
                var oldTail = workspace.OldTailPositions[i];
                
                header.Hash ^= ZobristTable.GetSnakeValue(i, newHead);
                grid.Snakes.Set(newHead);
                
                if (!workspace.HasEaten[i])
                {
                    header.Hash ^= ZobristTable.GetSnakeValue(i, oldTail);
                    grid.Snakes.Clear(oldTail);
                }
            }

            // Update food bitboard if food was eaten
            if (workspace.HasEaten[i] && !workspace.IsDead[i])
            {
                grid.Food.Clear(workspace.NewHeadPositions[i]);
            }
        }
    }

    // =================================================================
    // MCTS Expansion & Helper Methods
    // =================================================================
    
    /// <summary>
    /// Applies a single move for a single snake, used for MCTS tree expansion.
    /// </summary>
    public static void ApplySingleMove(ref WarArena arena, int snakeIndex, byte move)
    {
        var snake = arena.Snakes[snakeIndex];
        if (snake.Dead) return;

        ref var header = ref arena.Header;
        ref var grid = ref arena.Grid;
        
        var oldTail = snake.Tail;
        var head = snake.Head;
        
        // Branchless move calculation using the LUT.
        var newHead = arena.MovesLut.GetNeighbor(head, move);

        // Instant death check (wall or existing body)
        if (grid.IsOccupied(newHead))
        {
            KillSnake(ref arena, snakeIndex, ref header);
            return;
        }

        var hasEaten = grid.IsFood(newHead);
        var hazardDamage = grid.IsHazard(newHead) ? 15 : 0;
        var totalDamage = 1 + hazardDamage;

        snake.Move(newHead, hasEaten, totalDamage);

        // Starvation/hazard death check
        if (snake.Dead)
        {
            KillSnake(ref arena, snakeIndex, ref header);
            return;
        }

        // Update world state (bitboards and hash)
        header.Hash ^= ZobristTable.GetSnakeValue(snakeIndex, newHead);
        grid.Snakes.Set(newHead);

        if (!hasEaten)
        {
            header.Hash ^= ZobristTable.GetSnakeValue(snakeIndex, oldTail);
            grid.Snakes.Clear(oldTail);
        }
        else
        {
            grid.Food.Clear(newHead);
        }
    }

    /// <summary>
    /// Calculates the initial Zobrist hash for the entire game state.
    /// </summary>
    public static void InitializeHash(ref WarArena arena)
    {
        ref var header = ref arena.Header;
        var snakes = arena.Snakes;
        header.Hash = 0;
        
        for (var i = 0; i < snakes.Length; i++)
        {
            var snake = snakes[i];
            if (snake.Dead) continue;
            
            snake.GetSpans(out var span1, out var span2);
            foreach (var segment in span1) header.Hash ^= ZobristTable.GetSnakeValue(i, segment);
            foreach (var segment in span2) header.Hash ^= ZobristTable.GetSnakeValue(i, segment);
        }
    }

    /// <summary>
    /// Encapsulates the logic for removing a snake from the game state.
    /// </summary>
    private static void KillSnake(ref WarArena arena, int snakeIndex, ref WarArenaHeader header)
    {
        var snake = arena.Snakes[snakeIndex];
        if (snake.Dead) return; // Already dead, do nothing.

        snake.Kill();
        header.LiveSnakesCount--;

        // Remove snake from the board and update the hash
        snake.GetSpans(out var span1, out var span2);
        foreach (var segment in span1)
        {
            header.Hash ^= ZobristTable.GetSnakeValue(snakeIndex, segment);
            arena.Grid.Snakes.Clear(segment);
        }
        foreach (var segment in span2)
        {
            header.Hash ^= ZobristTable.GetSnakeValue(snakeIndex, segment);
            arena.Grid.Snakes.Clear(segment);
        }
    }
}