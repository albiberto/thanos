using System.Runtime.CompilerServices;

namespace Thanos.Hyper;

public static class HyperMoveGenerator
{
    /// <summary>
    /// Generates legal moves strictly based on the obstacles bitboard.
    /// Assumes 'AdvanceTail' has already been called for non-growing snakes.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte GetPlausibleMoves(ref Bitboard256 obstacles, byte headPosition)
    {
        byte moves = FastMoves.None;

        // Since the board is exactly 16 wide:
        // Y * 16 + X
        unchecked 
        {
            byte up    = (byte)(headPosition - 16);
            byte down  = (byte)(headPosition + 16);
            byte left  = (byte)(headPosition - 1);
            byte right = (byte)(headPosition + 1);

            // CPU pipelines will breeze through this without bounds checking.
            if (obstacles.IsUnset(up))    moves |= FastMoves.Up;
            if (obstacles.IsUnset(down))  moves |= FastMoves.Down;
            if (obstacles.IsUnset(left))  moves |= FastMoves.Left;
            if (obstacles.IsUnset(right)) moves |= FastMoves.Right;
        }

        return moves;
    }
}