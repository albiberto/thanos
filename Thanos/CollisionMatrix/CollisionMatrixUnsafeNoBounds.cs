using System.Runtime.CompilerServices;
using Thanos.Extensions;
using Thanos.Model;

namespace Thanos.CollisionMatrix;

// === VERSIONE UNSAFE ULTRA-OTTIMIZZATA ===
// Rimossi tutti i bounds check dato che l'API Battlesnake garantisce coordinate valide
// Benchmarking ha mostrato che per board grandi (19x19) e con 4 serpenti il tempo di esecuzione è ~17/18ns
public class CollisionMatrixUnsafeNoBounds
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
            for (var i = 0; i < hazardCount; i++)
            {
                var hazard = hazards[i];
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

                // === CORPO NEMICO ===
                for (var j = 0; j < enemyBodyLength; j++)
                {
                    var bodyPart = enemyBody[j];
                    *(ptr + bodyPart.y * width + bodyPart.x) = true;
                }

                // === HEAD-TO-HEAD PREVENTION ===
                if (enemyBodyLength > myBodyLength)
                {
                    var hx = enemyHead.x;
                    var hy = enemyHead.y;
                    var offset = hy * width + hx;

                    // Left
                    if (hx > 0)
                        *(ptr + offset - 1) = true;
                    // Right  
                    if (hx < width - 1)
                        *(ptr + offset + 1) = true;
                    // Up
                    if (hy > 0)
                        *(ptr + offset - width) = true;
                    // Down
                    if (hy < height - 1)
                        *(ptr + offset + width) = true;
                }
            }

            // === ME - CORPO (salta testa) ===
            var end = eat ? myBodyLength - 1 : myBodyLength;
            for (var i = 1; i < end; i++)
            {
                var bodyPart = myBody[i];
                *(ptr + bodyPart.y * width + bodyPart.x) = true;
            }
        }
    }
}