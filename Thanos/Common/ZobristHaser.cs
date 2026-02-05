using System.Numerics;
using Thanos.War;
using Thanos.War.State;

namespace Thanos.Common;

public static class ZobristHasher
{
    // In Thanos/Common/ZobristHasher.cs

    public static long CalculateHash(in GameState state)
    {
        long hash = 0;

        for (var i = 0; i < state.System.Count; i++)
        {
            var snake = state.System[i];
            if (snake.IsDead) continue;

            var snakeBodyBitboard = snake.Body.Buffer;
            hash = HashSnakeBitboard(hash, snakeBodyBitboard, i);

            // --- INIZIO FIX ---
            // Aggiungi HP e Lunghezza all'hash per renderlo univoco.
            // La soluzione Zobrist "pura" richiederebbe di espandere ZobristTable,
            // ma un hash/rotazione semplice è già un enorme miglioramento.

            // Combina l'hash con l'indice e la salute
            hash ^= i; // Assicura che l'HP del Serpente 0 sia diverso da quello del Serpente 1
            hash = long.RotateLeft(hash, 7);
            hash ^= snake.Hp;
            hash = long.RotateLeft(hash, 13);
            hash ^= snake.Length;
            hash = long.RotateLeft(hash, 11);
            // --- FINE FIX ---
        }

        var foodBitboard = state.Food.Buffer;
        hash = HashFoodBitboard(hash, foodBitboard);

        var hazardBitboard = state.Hazards.Buffer;
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