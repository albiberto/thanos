using System.Numerics;
using Thanos.Common;

namespace Thanos.MCST;

public static class Heuristics
{
    public static byte FindBestMove(byte legalMoves)
    {
        var count = BitOperations.PopCount(legalMoves);
        if (count == 0) return Moves.Up;

        var choice = Random.Shared.Next(count);
        byte mask = 1;
        while (true)
        {
            if ((legalMoves & mask) != 0) if (choice-- == 0) return mask;
            mask <<= 1;
        }
    }
}