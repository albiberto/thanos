using System.Runtime.CompilerServices;

namespace Thanos.LightSpeed;

public static class HyperMoveGenerator
{
    /// <summary>
    /// Generates a bitmask of legal moves based on the obstacles bitboard.
    /// Bit 0: Left, Bit 1: Right, Bit 2: Up, Bit 3: Down
    /// Assumes 'AdvanceTail' has already been called for non-growing snakes if calculating mid-turn.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte GetPlausibleMoves(ref Bitboard256 obstacles, byte headPosition)
    {
        byte moves = 0;

        unchecked 
        {
            // Calcolo coordinate usando la nostra topologia 16x16
            // In Battlesnake standard: Up aumenta la Y (+16), Down la diminuisce (-16)
            byte left  = (byte)(headPosition - 1);
            byte right = (byte)(headPosition + 1);
            byte up    = (byte)(headPosition + 16);
            byte down  = (byte)(headPosition - 16);

            // Costruiamo la bitmask usando le costanti di HyperMoves (0, 1, 2, 3)
            // Se il bit nella mappa ostacoli è 0 (IsUnset), la mossa è legale.
            if (obstacles.IsUnset(left))  moves |= (byte)(1 << HyperMoves.Left);
            if (obstacles.IsUnset(right)) moves |= (byte)(1 << HyperMoves.Right);
            if (obstacles.IsUnset(up))    moves |= (byte)(1 << HyperMoves.Up);
            if (obstacles.IsUnset(down))  moves |= (byte)(1 << HyperMoves.Down);
        }

        return moves;
    }
}