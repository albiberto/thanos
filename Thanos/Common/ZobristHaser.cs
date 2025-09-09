using System.Numerics;
using Thanos.War;

namespace Thanos.Common;

public static class ZobristHasher
{
    public static long CalculateHash(in Arena arena)
    {
        long hash = 0;
        var grid = arena.Grid;

        // --- 1. Hashing dei Serpenti ---
        for (var i = 0; i < arena.System.Count; i++)
        {
            var snake = arena.System[i];
            if (snake.IsDead) continue;

            // FIX 1: Usiamo la nuova proprietà RawUlongData
            var snakeBodyBitboard = snake.Body.Memory;
            hash = HashSnakeBitboard(hash, snakeBodyBitboard, i);
        }

        // --- 2. Hashing del Cibo ---
        var foodBitboard = grid.Food.Memory;
        hash = HashFoodBitboard(hash, foodBitboard);

        // --- 3. Hashing degli Ostacoli ---
        var hazardBitboard = grid.Hazards.Memory;
        // FIX 2: Chiamiamo il nuovo metodo helper per gli ostacoli
        hash = HashHazardBitboard(hash, hazardBitboard);

        return hash;
    }

    // Metodo helper per i SERPENTI
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

    // Metodo helper per il CIBO
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

    // FIX 2: Nuovo metodo helper per gli OSTACOLI
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