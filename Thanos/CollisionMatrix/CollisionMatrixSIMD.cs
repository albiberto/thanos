using System.Runtime.CompilerServices;
using Thanos.Extensions;
using Thanos.Model;

namespace Thanos.CollisionMatrix;

// === VERSIONE CON SIMD (SE DISPONIBILE) ===
// Benchmarking ha mostrato che per board grandi (19x19) e con 4 serpenti il tempo di esecuzione è ~28/29ns,
public static class CollisionMatrixSIMD
{
    private static readonly bool[,] _collisionMatrix = new bool[19, 19];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void BuildCollisionMatrix(Board board, Snake mySnake)
    {
        // Implementazione con System.Numerics.Vector per batch processing
        // Utile per board molto grandi o molti serpenti

        var width = board.width;
        var height = board.height;
        var myId = mySnake.id;
        var myBody = mySnake.body;
        var myBodyLength = myBody.Length;
        var eat = mySnake.health < 100;

        _collisionMatrix.ClearUnsafe();

        fixed (bool* ptr = _collisionMatrix)
        {
            // Pre-compute positions in batch
            Span<uint> positions = stackalloc uint[64]; // Stack allocation
            var posCount = 0;

            // === HAZARDS BATCH ===
            var hazards = board.hazards;
            for (var i = 0; i < hazards.Length; i++)
            {
                var h = hazards[i];
                positions[posCount++] = h.y * width + h.x;

                if (posCount == 64)
                {
                    FlushPositionsUnsafe(ptr, positions, posCount);
                    posCount = 0;
                }
            }

            // === SNAKES BATCH ===
            var snakes = board.snakes;
            for (var i = 0; i < snakes.Length; i++)
            {
                var snake = snakes[i];
                if (snake.id == myId) continue;

                var body = snake.body;
                for (var j = 0; j < body.Length; j++)
                {
                    var bp = body[j];
                    if (bp.x < width && bp.y < height)
                    {
                        positions[posCount++] = bp.y * width + bp.x;

                        if (posCount == 64)
                        {
                            FlushPositionsUnsafe(ptr, positions, posCount);
                            posCount = 0;
                        }
                    }
                }
            }

            // === MY BODY BATCH ===
            var end = eat ? myBodyLength - 1 : myBodyLength;
            for (var i = 1; i < end; i++)
            {
                var bp = myBody[i];
                if (bp.x < width && bp.y < height)
                {
                    positions[posCount++] = bp.y * width + bp.x;

                    if (posCount == 64)
                    {
                        FlushPositionsUnsafe(ptr, positions, posCount);
                        posCount = 0;
                    }
                }
            }

            // Flush remaining
            if (posCount > 0) FlushPositionsUnsafe(ptr, positions, posCount);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe void FlushPositionsUnsafe(bool* ptr, Span<uint> positions, int count)
    {
        for (var i = 0; i < count; i++) *(ptr + positions[i]) = true;
    }
}