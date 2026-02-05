using System.Numerics;
using System.Runtime.CompilerServices;
using Thanos.Shared;
using Thanos.War.State;

namespace Thanos.War.Rules;

/// <summary>
///     PURE PHYSICS ENGINE.
///     Determines the outcome of moves. Deterministic.
/// </summary>
public static class StandardRules
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SimulateTurn(ref GameState state, ReadOnlySpan<byte> moves, int hazardDamage)
    {
        // ... (Il codice di SimulateTurn rimane identico a prima) ...
        // Copia qui SOLO il metodo SimulateTurn che abbiamo scritto nel passo precedente.
        // Niente euristiche qui.

        // Stack allocation ensures zero GC pressure during simulation.
        Span<ushort> nextHeads = stackalloc ushort[Constants.MaxSnakesCount];
        Span<ushort> tails = stackalloc ushort[Constants.MaxSnakesCount];
        Span<int> lengths = stackalloc int[Constants.MaxSnakesCount];

        var aliveMask = 0;
        var deadMask = 0;
        var eatMask = 0;
        var ateMask = 0; // Mask for snakes that ate LAST turn (Pending Growth)

        // --- PHASE 1: Snapshot & Move Calculation ---
        for (var snakeIndex = 0; snakeIndex < state.System.Count; snakeIndex++)
        {
            var snake = state.System[snakeIndex];

            if (snake.IsDead) continue;

            // Resolve next position using the lookup matrix
            var nextHead = state.Neighbors.Get(snake.Head, moves[snakeIndex]);

            // Cache vital data
            nextHeads[snakeIndex] = nextHead;
            tails[snakeIndex] = snake.Tail;
            lengths[snakeIndex] = snake.Length;

            aliveMask |= 1 << snakeIndex;

            // If growth is pending (ate last turn), tail is anchored
            if (snake.IsGrowthPending) ateMask |= 1 << snakeIndex;

            // Immediate Collisions (Walls)
            if (NeighborsMatrix.IsOutOfBound(nextHead))
                deadMask |= 1 << snakeIndex;
            else if (state.Food.IsSet(nextHead))
                eatMask |= 1 << snakeIndex;
        }

        // --- PHASE 2: Body Collision Resolution (Tail Chasing) ---
        var wallsSurvivorsMask = aliveMask & ~deadMask;

        // A tail moves (clearing the cell) if:
        // 1. The snake is alive, NOT eating NOW, and NOT growing (ate previously).
        // 2. The snake died (entire body removed).
        var movingTailsMask = (wallsSurvivorsMask & ~eatMask & ~ateMask) | deadMask;

        var scanner = wallsSurvivorsMask;
        while (scanner != 0)
        {
            var snakeIndex = BitOperations.TrailingZeroCount(scanner);
            scanner &= scanner - 1;

            var nextHead = nextHeads[snakeIndex];

            // If global board says free, we are safe (for now)
            if (state.Snakes.IsUnset(nextHead)) continue;

            // If occupied, check if it's a "Moving Tail"
            var isMovingTail = false;
            var potentialMovers = movingTailsMask;
            while (potentialMovers != 0)
            {
                var j = BitOperations.TrailingZeroCount(potentialMovers);
                potentialMovers &= potentialMovers - 1;

                if (tails[j] != nextHead) continue;

                isMovingTail = true;
                break;
            }

            if (!isMovingTail) deadMask |= 1 << snakeIndex;
        }

        // --- PHASE 3: Head-to-Head Resolution ---
        var collisionSurvivors = aliveMask & ~deadMask;

        // Check only if 2+ snakes are potentially colliding
        if (collisionSurvivors != 0 && (collisionSurvivors & (collisionSurvivors - 1)) != 0)
        {
            var outer = collisionSurvivors;
            while (outer != 0)
            {
                var snakeId = BitOperations.TrailingZeroCount(outer);
                outer &= outer - 1;

                var headA = nextHeads[snakeId];
                var lenA = lengths[snakeId];

                var inner = outer; // Triangular check
                while (inner != 0)
                {
                    var enemyIndex = BitOperations.TrailingZeroCount(inner);
                    inner &= inner - 1;

                    if (headA != nextHeads[enemyIndex]) continue;

                    var lenB = lengths[enemyIndex];
                    // Equal or smaller -> Death
                    if (lenA <= lenB) deadMask |= 1 << snakeId;
                    if (lenB <= lenA) deadMask |= 1 << enemyIndex;
                }
            }
        }

        // --- PHASE 4: Two-Pass Commit ---
        // 1. Updates & Clears (Must happen before setting new heads)
        var survivorsMask = 0;
        var commitScanner = aliveMask;

        while (commitScanner != 0)
        {
            var snakeId = BitOperations.TrailingZeroCount(commitScanner);
            commitScanner &= commitScanner - 1;

            var snake = state.System[snakeId];

            if ((deadMask & (1 << snakeId)) != 0)
            {
                // Death: Remove body from global map
                state.Snakes.Xor(snake.Body);
                snake.Kill();
            }
            else
            {
                // Survivor
                var eating = (eatMask & (1 << snakeId)) != 0;
                var damage = state.Hazards.IsSet(nextHeads[snakeId]) ? hazardDamage : 0;

                snake.UpdateAfterMove(nextHeads[snakeId], eating, damage + 1);

                if (eating)
                    state.Food.Unset(nextHeads[snakeId]);
                else if ((ateMask & (1 << snakeId)) == 0)
                    // Tail moves -> Clear bit
                    state.Snakes.Unset(tails[snakeId]);

                survivorsMask |= 1 << snakeId;
            }
        }

        // 2. Set New Heads (Absolute priority)
        while (survivorsMask != 0)
        {
            var snakeIndex = BitOperations.TrailingZeroCount(survivorsMask);
            survivorsMask &= survivorsMask - 1;
            state.Snakes.Set(nextHeads[snakeIndex]);
        }
    }
}