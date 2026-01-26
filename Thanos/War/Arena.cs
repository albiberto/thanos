using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using Thanos.Common;
using Thanos.Shared;
using Thanos.SourceGen;
using Thanos.War.Structures;

namespace Thanos.War;

public readonly ref struct Arena(
    SnakesSystem system,
    Bitboard food,
    Bitboard hazards,
    Bitboard snakes,
    NeighborsMatrix neighborsMatrix)
{
    public readonly SnakesSystem System = system;
    public readonly Bitboard Food = food;
    public readonly Bitboard Hazards = hazards;
    public readonly Bitboard Snakes = snakes;

    private readonly NeighborsMatrix _neighborsMatrix = neighborsMatrix;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void InitializeFromRequest(in Request request, ReadOnlySpan<string> orderedIds)
    {
        System.Initialize();
        Food.Clear();
        Hazards.Clear();
        Snakes.Clear();

        var board = request.Board;

        foreach (var snakeData in board.Snakes)
            for (var i = 0; i < orderedIds.Length; i++)
            {
                // Ordinal comparison is mandatory for speed and correctness
                if (!string.Equals(orderedIds[i], snakeData.Id, StringComparison.Ordinal)) continue;

                var snake = System[i];
                snake.Initialize(snakeData);
                Snakes.Or(snake.Body);
                break;
            }

        foreach (var coordinate in board.Food) Food.Set(coordinate);
        foreach (var coordinate in board.Hazards) Hazards.Set(coordinate);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CloneFrom(in Arena source)
    {
        System.CopyFrom(in source.System);
        Food.CopyFrom(in source.Food);
        Hazards.CopyFrom(in source.Hazards);
        Snakes.CopyFrom(in source.Snakes);
    }

    // --- SIMULATION ENGINE ---

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SimulateTurn(ReadOnlySpan<byte> moves, int hazardDamage)
    {
        Span<ushort> nextHeads = stackalloc ushort[Constants.MaxSnakesCount];
        Span<ushort> tails = stackalloc ushort[Constants.MaxSnakesCount];
        Span<int> lengths = stackalloc int[Constants.MaxSnakesCount];

        var aliveMask = 0;
        var deadMask = 0;
        var eatMask = 0;
        var stackedMask = 0;

        // --- PHASE 1: Snapshot & Move Calculation ---
        // GOAL: Calculate intended moves and build bitmasks for fast logic.
        //       Do NOT modify the board state yet.
        for (var snakeIndex = 0; snakeIndex < System.Count; snakeIndex++)
        {
            var snake = System[snakeIndex];

            // 1. Skip dead snakes immediately to save cycles
            if (snake.IsDead) continue;

            // 2. Resolve the destination coordinate (Next Head)
            //    Using the pre-calculated neighbors matrix avoids costly X/Y math.
            var nextHead = _neighborsMatrix.Get(snake.Head, moves[snakeIndex]);

            // 3. Cache vital data onto the Stack (Hot Path Optimization)
            //    Accessing these arrays later is faster than dereferencing the Snake object again.
            nextHeads[snakeIndex] = nextHead;
            tails[snakeIndex] = snake.Tail;
            lengths[snakeIndex] = snake.Length;

            // 4. Build the Bitmasks (The "Registers" of our CPU)

            // Mark as alive for the simulation
            aliveMask |= 1 << snakeIndex;

            // Check if the tail is "Stacked" (Pending Growth).
            // If TRUE, the tail is anchored and will NOT move even if we don't eat.
            if (snake.IsGrowthPending) stackedMask |= 1 << snakeIndex;

            // 5. Evaluate Immediate Consequences
            if (NeighborsMatrix.IsOutOfBound(nextHead))
                // Immediate Death: Wall collision.
                // We flag this now so we don't waste time checking head-to-head for them.
                deadMask |= 1 << snakeIndex;
            else if (Food.IsSet(nextHead))
                // Eating: The snake will grow, and the tail will NOT move.
                // This is crucial for the Tail-Chasing phase.
                eatMask |= 1 << snakeIndex;
        }

        // --- FASE 2: Risoluzione Collisioni Corpi (Tail Chasing) ---

        // wallsSurvivors: Serpenti vivi che NON si sono schiantati sui muri.
        var wallsSurvivorsMask = aliveMask & ~deadMask;

        // movingTailsMask: Sottoinsieme dei sopravvissuti che NON mangiano.
        // Solo le code di questi serpenti libereranno la casella occupata.
        var movingTailsMask = wallsSurvivorsMask & ~eatMask;

        while (wallsSurvivorsMask != 0)
        {
            var snakeIndex = BitOperations.TrailingZeroCount(wallsSurvivorsMask);
            wallsSurvivorsMask &= wallsSurvivorsMask - 1;

            var nextHead = nextHeads[snakeIndex];

            // Se la casella è libera, siamo salvi.
            if (Snakes.IsUnset(nextHead)) continue;

            // Se la casella è occupata, potremmo venir salvati da una coda che si sposta, cerchiamola
            var isMovingTail = false;

            // Check against all potential moving tails
            while (movingTailsMask != 0)
            {
                var j = BitOperations.TrailingZeroCount(movingTailsMask);
                movingTailsMask &= movingTailsMask - 1;

                if (tails[j] != nextHead) continue;
                isMovingTail = true;
                break;
            }

            if (!isMovingTail) deadMask |= 1 << i;
        }

        // --- PHASE 3: Head-to-Head Resolution ---
        var collisionSurvivors = aliveMask & ~deadMask;

        // Check only if at least 2 snakes are potentially colliding
        if (collisionSurvivors != 0 && (collisionSurvivors & (collisionSurvivors - 1)) != 0)
        {
            var outer = collisionSurvivors;
            while (outer != 0)
            {
                var snakeId = BitOperations.TrailingZeroCount(outer);
                outer &= outer - 1;

                var headA = nextHeads[snakeId];
                var lenA = lengths[snakeId];

                var inner = outer; // Check only against subsequent snakes (triangle check)
                while (inner != 0)
                {
                    var enemyIndex = BitOperations.TrailingZeroCount(inner);
                    inner &= inner - 1;

                    if (headA != nextHeads[enemyIndex]) continue;

                    var lenB = lengths[enemyIndex];
                    if (lenA <= lenB) deadMask |= 1 << snakeId;
                    if (lenB <= lenA) deadMask |= 1 << enemyIndex;
                }
            }
        }

        // --- PHASE 4: Two-Pass Commit ---
        // Heads must be set AFTER tails are cleared to prevent
        // Snake A (moving to X) having its head cleared by Snake B (leaving X).

        var survivorsMask = 0;

        // Pass A: Deaths, Internal Updates, Tail Clears
        var commitMask = aliveMask;
        while (commitMask != 0)
        {
            var i = BitOperations.TrailingZeroCount(commitMask);
            commitMask &= commitMask - 1;

            var snake = System[i];

            if ((deadMask & (1 << i)) != 0)
            {
                // Death: Remove entire body from global bitboard
                Snakes.Xor(snake.Body);
                snake.Kill();
            }
            else
            {
                // Survivor
                var eating = (eatMask & (1 << i)) != 0;
                var damage = Hazards.IsSet(nextHeads[i]) ? hazardDamage : 0;

                // 1. Update internal state
                // Standard Rules: 1 HP decay per turn + Hazard damage
                snake.UpdateAfterMove(nextHeads[i], eating, damage + 1);

                switch (eating)
                {
                    // 2. Global Board: Remove Old Tail
                    // Only if not eating AND not previously stacked/anchored
                    case false when (stackedMask & (1 << i)) == 0:
                        Snakes.Unset(tails[i]);
                        break;
                    case true:
                        Food.Unset(nextHeads[i]);
                        break;
                }

                survivorsMask |= 1 << i;
            }
        }

        // Pass B: Set New Heads
        while (survivorsMask != 0)
        {
            var snakeIndex = BitOperations.TrailingZeroCount(survivorsMask);
            survivorsMask &= survivorsMask - 1;

            // Set Head (Absolute Priority: Head always overwrites empty space)
            Snakes.Set(nextHeads[snakeIndex]);
        }
    }

    // --- HEURISTIC HELPERS ---

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte GetPlausibleMoves(int index)
    {
        var snake = System[index];
        if (snake.IsDead) return 0;

        // 1. SIMD Fetch of all neighbors
        var neighbors = _neighborsMatrix.GetAll(snake.Head);

        // 2. Extract elements
        var up = neighbors.GetElement(0);
        var down = neighbors.GetElement(1);
        var left = neighbors.GetElement(2);
        var right = neighbors.GetElement(3);

        // 3. Compute Basic Legality (Walls & Body)
        // Tail is safe if we are not growing (checked inside GetLegalMoves via logic)
        // Assuming GetLegalMoves handles the 'Food' check for tail safety
        var isUnrolled = !snake.IsGrowthPending && snake.Tail != snake.PreTail;
        var mask = GetLegalMoves(up, down, left, right, snake.Tail, isUnrolled);

        // 4. Filter Risky Moves (Head-to-Head & Dead Ends)
        // Only run if we have moves to filter
        return mask == 0 ? (byte)0 : FilterRiskyMoves(neighbors, mask, index, snake.Length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private byte GetLegalMoves(ushort up, ushort down, ushort left, ushort right, ushort tail, bool isUnrolled)
    {
        byte moves = 0;

        // Unroll logic: If isUnrolled is true, the tail WILL move, so the spot is safe.
        // BUT, if there is food on the tail (rare edge case), we eat and grow -> tail stays -> NOT safe.
        // Food check is added to the tail-safety condition.

        if (NeighborsMatrix.IsValid(up))
            if (Snakes.IsUnset(up) || (up == tail && isUnrolled && Food.IsUnset(up)))
                moves |= Moves.Up;

        if (NeighborsMatrix.IsValid(down))
            if (Snakes.IsUnset(down) || (down == tail && isUnrolled && Food.IsUnset(down)))
                moves |= Moves.Down;

        if (NeighborsMatrix.IsValid(left))
            if (Snakes.IsUnset(left) || (left == tail && isUnrolled && Food.IsUnset(left)))
                moves |= Moves.Left;

        if (NeighborsMatrix.IsValid(right))
            if (Snakes.IsUnset(right) || (right == tail && isUnrolled && Food.IsUnset(right)))
                moves |= Moves.Right;

        return moves;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private byte FilterRiskyMoves(Vector64<ushort> myNeighbors, byte mask, int myIndex, int myLength)
    {
        // Check UP
        if ((mask & Moves.Up) != 0 && !IsMoveSafeDynamic(myNeighbors.GetElement(0), myIndex, myLength))
            mask ^= Moves.Up;

        // Check DOWN
        if ((mask & Moves.Down) != 0 && !IsMoveSafeDynamic(myNeighbors.GetElement(1), myIndex, myLength))
            mask ^= Moves.Down;

        // Check LEFT
        if ((mask & Moves.Left) != 0 && !IsMoveSafeDynamic(myNeighbors.GetElement(2), myIndex, myLength))
            mask ^= Moves.Left;

        // Check RIGHT
        if ((mask & Moves.Right) != 0 && !IsMoveSafeDynamic(myNeighbors.GetElement(3), myIndex, myLength))
            mask ^= Moves.Right;

        return mask;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsMoveSafeDynamic(ushort targetHead, int myIndex, int myLength)
    {
        // 1. Fetch Neighbors of the TARGET tile (Depth 1 lookahead)
        var targetNeighbors = _neighborsMatrix.GetAll(targetHead);

        // --- STEP A: HEAD-TO-HEAD (Suicide Check) ---
        for (var i = 0; i < System.Count; i++)
        {
            if (i == myIndex) continue;
            var enemy = System[i];

            // If enemy is smaller, they are not a threat (we kill them)
            // If enemy is equal, we both die (usually bad, treated as unsafe)
            if (enemy.IsDead || enemy.Length < myLength) continue;

            // Broadcast enemy head to vector
            var vEnemyHead = Vector64.Create(enemy.Head);

            // SIMD Comparison: check if any of target's neighbors == enemy head
            if (Vector64.Equals(targetNeighbors, vEnemyHead) != Vector64<ushort>.Zero)
                return false;
        }

        // --- STEP B: DEAD END (Flood Fill Depth 1) ---
        // We need at least one open neighbor to not be immediately trapped
        // Unrolling loop for scalar checks against Bitboard

        // 0
        var n = targetNeighbors.GetElement(0);
        if (NeighborsMatrix.IsValid(n) && Snakes.IsUnset(n)) return true;

        // 1
        n = targetNeighbors.GetElement(1);
        if (NeighborsMatrix.IsValid(n) && Snakes.IsUnset(n)) return true;

        // 2
        n = targetNeighbors.GetElement(2);
        if (NeighborsMatrix.IsValid(n) && Snakes.IsUnset(n)) return true;

        // 3
        n = targetNeighbors.GetElement(3);
        if (NeighborsMatrix.IsValid(n) && Snakes.IsUnset(n)) return true;

        return false;
    }

    public void SimulateRandomFoodSpawn(int foodSpawnChance, int minimumFood, int area)
    {
        if (Food.PopCount() >= minimumFood && Random.Shared.Next(0, 100) >= foodSpawnChance) return;

        for (var i = 0; i < 10; i++)
        {
            var spot = (ushort)Random.Shared.Next(0, area);
            if (Snakes.IsSet(spot) || Food.IsSet(spot)) continue;

            Food.Set(spot);
            break;
        }
    }
}