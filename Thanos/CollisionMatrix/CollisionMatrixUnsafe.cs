using System.Runtime.CompilerServices;
using Thanos.Extensions;
using Thanos.Model;

namespace Thanos.CollisionMatrix;

// === VERSIONE UNSAFE OTTIMIZZATA ===
// Benchmarking ha mostrato che per board grandi (19x19) e con 4 serpenti il tempo di esecuzione è ~20ns,
public static class CollisionMatrixUnsafe
{
    private static readonly bool[,] _collisionMatrix = new bool[19, 19];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void BuildCollisionMatrix(Board board, Snake mySnake)
    {
        var width = board.width;
        var height = board.height;

        var hazards = board.hazards;
        var hazardCount = hazards.Length;

        var snakes = board.snakes;
        var snakeCount = snakes.Length;

        var myId = mySnake.id;
        var myBody = mySnake.body;
        var myBodyLength = myBody.Length;
        var eat = mySnake.health < 100;

        _collisionMatrix.ClearUnsafe();

        fixed (bool* ptr = _collisionMatrix)
        {
            // === HAZARDS ===
            if (hazardCount > 0)
                for (var i = 0; i < hazardCount; i++)
                {
                    var hazard = hazards[i];
                    // FIX: era hazard.x * 19 + hazard.y (hardcoded 19!)
                    // Ora usa width dinamico
                    *(ptr + hazard.y * width + hazard.x) = true;
                }

            // === SERPENTI NEMICI ===
            for (var i = 0; i < snakeCount; i++)
            {
                var snake = snakes[i];
                if (snake.id == myId) continue;

                var enemyHead = snake.head;
                var enemyBody = snake.body;
                var enemyBodyLength = enemyBody.Length;

                // === CORPO NEMICO - ACCESSO DIRETTO ===
                for (var j = 0; j < enemyBodyLength; j++)
                {
                    var bodyPart = enemyBody[j];
                    var x = bodyPart.x;
                    var y = bodyPart.y;

                    // Bounds check una volta - più veloce
                    if (x < width && y < height)
                        *(ptr + y * width + x) = true;
                }

                // === HEAD-TO-HEAD PREVENTION ===
                if (enemyBodyLength > myBodyLength)
                {
                    var hx = enemyHead.x;
                    var hy = enemyHead.y;

                    var offset = hy * width + hx;

                    if (hx > 0)
                        *(ptr + offset - 1) = true; // Left
                    if (hx < width - 1)
                        *(ptr + offset + 1) = true; // Right
                    if (hy > 0)
                        *(ptr + offset - width) = true; // Up
                    if (hy < height - 1)
                        *(ptr + offset + width) = true; // Down
                }
            }

            // === ME - VERSIONE UNSAFE CORRETTA ===
            var end = eat ? myBodyLength - 1 : myBodyLength;

            // Solo corpo (salta testa) - la testa si muoverà
            for (var i = 1; i < end; i++)
            {
                var bodyPart = myBody[i];
                var x = bodyPart.x;
                var y = bodyPart.y;

                if (x < width && y < height)
                    *(ptr + y * width + x) = true;
            }
        }
    }
}