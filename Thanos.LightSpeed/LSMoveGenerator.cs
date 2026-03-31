using System.Runtime.CompilerServices;

namespace Thanos.LightSpeed;

public static class LSMoveGenerator
{
    /// <summary>
    /// Extracts the lowest valid move index from the move mask and unsets it.
    /// Returns 0=Left, 1=Right, 2=Up, 3=Down.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte PopNextMove(ref byte moveMask)
    {
        // Trova l'indice del primo bit a 1 (0, 1, 2 o 3)
        byte moveIndex = (byte)System.Numerics.BitOperations.TrailingZeroCount(moveMask);
    
        // Azzera il bit appena letto (Trick di Brian Kernighan)
        moveMask &= (byte)(moveMask - 1); 
    
        return moveIndex;
    }
    
    /// <summary>
    /// Generates a bitmask of legal moves based on the obstacles bitboard.
    /// Bit 0: Left, Bit 1: Right, Bit 2: Up, Bit 3: Down
    /// Assumes 'AdvanceTail' has already been called for non-growing snakes if calculating mid-turn.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte GetPlausibleMoves(ref LSBitboard obstacles, byte headPosition)
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
            if (obstacles.IsUnset(left))  moves |= (byte)(1 << LSMoves.Left);
            if (obstacles.IsUnset(right)) moves |= (byte)(1 << LSMoves.Right);
            if (obstacles.IsUnset(up))    moves |= (byte)(1 << LSMoves.Up);
            if (obstacles.IsUnset(down))  moves |= (byte)(1 << LSMoves.Down);
        }

        return moves;
    }
}