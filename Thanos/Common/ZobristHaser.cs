using System.Numerics;
using Thanos.War;

namespace Thanos.Common;

public static class ZobristHasher
{
    public static long CalculateHash(in Arena arena)
    {
        long hash = 0;

        for (var i = 0; i < arena.System.Count; i++)
        {
            var snake = arena.System[i];
            if (snake.IsDead) continue;

            var snakeBodyBitboard = snake.Body.Buffer;
            hash = HashSnakeBitboard(hash, snakeBodyBitboard, i);
        }

        var foodBitboard = arena.Food.Buffer;
        hash = HashFoodBitboard(hash, foodBitboard);
    
        var hazardBitboard = arena.Hazards.Buffer;
        hash = HashHazardBitboard(hash, hazardBitboard);

        return hash;
    }

    private static long HashSnakeBitboard(long currentHash, ReadOnlySpan<ulong> bitboard, int snakeIndex)
    {
        for (var i = 0; i < bitboard.Length; i++)
        {
            var chunk = bitboard[i];
            if (chunk == 0) continue;
            while (chunk != 0)
            {
                var bitIndex = BitOperations.TrailingZeroCount(chunk);
                var pos1D = (ushort)((i << 6) + bitIndex);
                currentHash ^= ZobristTable.GetSnakeValue(snakeIndex, pos1D);
                chunk &= ~(1UL << bitIndex);
            }
        }

        return currentHash;
    }

    private static long HashFoodBitboard(long currentHash, ReadOnlySpan<ulong> bitboard)
    {
        for (var i = 0; i < bitboard.Length; i++)
        {
            var chunk = bitboard[i];
            if (chunk == 0) continue;
            while (chunk != 0)
            {
                var bitIndex = BitOperations.TrailingZeroCount(chunk);
                var pos1D = (ushort)((i << 6) + bitIndex);
                currentHash ^= ZobristTable.GetFoodValue(pos1D);
                chunk &= ~(1UL << bitIndex);
            }
        }

        return currentHash;
    }
    
    private static long HashHazardBitboard(long currentHash, ReadOnlySpan<ulong> bitboard)
    {
        for (var i = 0; i < bitboard.Length; i++)
        {
            var chunk = bitboard[i];
            if (chunk == 0) continue;
            while (chunk != 0)
            {
                var bitIndex = BitOperations.TrailingZeroCount(chunk);
                var pos1D = (ushort)((i << 6) + bitIndex);
                currentHash ^= ZobristTable.GetHazardValue(pos1D);
                chunk &= ~(1UL << bitIndex);
            }
        }

        return currentHash;
    }
}