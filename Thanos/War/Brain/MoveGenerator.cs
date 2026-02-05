using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using Thanos.Common;
using Thanos.Shared;
using Thanos.War.State;

namespace Thanos.War.Brain;

/// <summary>
///     Responsible for generating valid moves and pruning obviously bad ones (Heuristics).
///     Logic: State -> Possible Moves Mask
/// </summary>
public static class MoveGenerator
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte GetPlausibleMoves(ref GameState state, int index)
    {
        var snake = state.System[index];
        if (snake.IsDead) return 0;

        var neighbors = state.Neighbors.GetAll(snake.Head);

        var up = neighbors.GetElement(0);
        var down = neighbors.GetElement(1);
        var left = neighbors.GetElement(2);
        var right = neighbors.GetElement(3);

        // Tail Unrolling Logic
        var isUnrolled = !snake.IsGrowthPending && snake.Tail != snake.PreTail;

        var mask = GetLegalMoves(ref state, up, down, left, right, snake.Tail, isUnrolled);

        return mask == 0 ? (byte)0 : FilterRiskyMoves(ref state, neighbors, mask, index, snake.Length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte GetLegalMoves(ref GameState state, ushort up, ushort down, ushort left, ushort right, ushort tail, bool isUnrolled)
    {
        byte moves = 0;

        // Check wall/body collisions
        if (NeighborsMatrix.IsValid(up))
            if (state.Snakes.IsUnset(up) || (up == tail && isUnrolled && state.Food.IsUnset(up)))
                moves |= Moves.Up;

        if (NeighborsMatrix.IsValid(down))
            if (state.Snakes.IsUnset(down) || (down == tail && isUnrolled && state.Food.IsUnset(down)))
                moves |= Moves.Down;

        if (NeighborsMatrix.IsValid(left))
            if (state.Snakes.IsUnset(left) || (left == tail && isUnrolled && state.Food.IsUnset(left)))
                moves |= Moves.Left;

        if (NeighborsMatrix.IsValid(right))
            if (state.Snakes.IsUnset(right) || (right == tail && isUnrolled && state.Food.IsUnset(right)))
                moves |= Moves.Right;

        return moves;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte FilterRiskyMoves(ref GameState state, Vector64<ushort> myNeighbors, byte mask, int myIndex, int myLength)
    {
        // Filter moves that lead to immediate Head-to-Head death or Dead Ends (Depth 1)

        if ((mask & Moves.Up) != 0 && !IsMoveSafeDynamic(ref state, myNeighbors.GetElement(0), myIndex, myLength))
            mask ^= Moves.Up;

        if ((mask & Moves.Down) != 0 && !IsMoveSafeDynamic(ref state, myNeighbors.GetElement(1), myIndex, myLength))
            mask ^= Moves.Down;

        if ((mask & Moves.Left) != 0 && !IsMoveSafeDynamic(ref state, myNeighbors.GetElement(2), myIndex, myLength))
            mask ^= Moves.Left;

        if ((mask & Moves.Right) != 0 && !IsMoveSafeDynamic(ref state, myNeighbors.GetElement(3), myIndex, myLength))
            mask ^= Moves.Right;

        return mask;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsMoveSafeDynamic(ref GameState state, ushort targetHead, int myIndex, int myLength)
    {
        var targetNeighbors = state.Neighbors.GetAll(targetHead);

        // 1. HEAD-TO-HEAD Check (Avoid suicide against larger/equal snakes)
        for (var i = 0; i < state.System.Count; i++)
        {
            if (i == myIndex) continue;
            var enemy = state.System[i];

            if (enemy.IsDead || enemy.Length < myLength) continue;

            var vEnemyHead = Vector64.Create(enemy.Head);

            if (Vector64.Equals(targetNeighbors, vEnemyHead) != Vector64<ushort>.Zero)
                return false;
        }

        // 2. FLOOD FILL Check (Depth 1) - Avoid stepping into a single cell with no exit
        var n0 = targetNeighbors.GetElement(0);
        if (NeighborsMatrix.IsValid(n0) && state.Snakes.IsUnset(n0)) return true;

        var n1 = targetNeighbors.GetElement(1);
        if (NeighborsMatrix.IsValid(n1) && state.Snakes.IsUnset(n1)) return true;

        var n2 = targetNeighbors.GetElement(2);
        if (NeighborsMatrix.IsValid(n2) && state.Snakes.IsUnset(n2)) return true;

        var n3 = targetNeighbors.GetElement(3);
        if (NeighborsMatrix.IsValid(n3) && state.Snakes.IsUnset(n3)) return true;

        return false;
    }
}