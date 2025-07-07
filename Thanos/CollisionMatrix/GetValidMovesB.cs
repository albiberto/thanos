using System.Runtime.CompilerServices;
using Thanos.Domain;
using Thanos.Enums;
using Thanos.Extensions;
using Thanos.Model;

namespace Thanos.CollisionMatrix;

public static class GetValidMovesB
{
    private static readonly bool[,] _collisionMatrix = new bool[19, 19];
    private static readonly Direction[] _validMoves = new Direction[4];
    
     public static unsafe int GetValidMoves(uint width, uint height, string myId, Point[] myBody, int myBodyLength, uint myHeadX, uint myHeadY, Point[] hazards, int hazardCount, Snake[] snakes, int snakeCount, bool eat)
    {
        BuildCollisionMatrix(width, height, myId, myBody, myBodyLength, hazards, hazardCount, snakes, snakeCount, eat);
        
        // TODO: DEBUG - Stampa matrice di collisione
        // _collisionMatrix.Print(width, height);
        
        var count = 0;

        fixed (bool* ptr = _collisionMatrix)
        {
            // === UP ===
            if (myHeadY > 0)
            {
                var checkRow = myHeadY - 1;
                if (checkRow < height && !*(ptr + checkRow * width + myHeadX)) _validMoves[count++] = Direction.Up;
            }

            // === RIGHT ===
            if (myHeadX + 1 < width)
            {
                var checkCol = myHeadX + 1;
                if (!*(ptr + myHeadY * width + checkCol)) _validMoves[count++] = Direction.Right;
            }

            // === DOWN ===
            if (myHeadY + 1 < height)
            {
                var checkRow = myHeadY + 1;
                if (!*(ptr + checkRow * width + myHeadX)) _validMoves[count++] = Direction.Down;
            }

            // === LEFT ===
            if (myHeadX > 0)
            {
                var checkCol = myHeadX - 1;
                if (checkCol < width && !*(ptr + myHeadY * width + checkCol)) _validMoves[count++] = Direction.Left;
            }
        }

        return count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void BuildCollisionMatrix(uint width, uint height, string myId, Point[] myBody, int myBodyLength, Point[] hazards, int hazardCount, Snake[] snakes, int snakeCount, bool eat)
    {
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

            // === ME - CORPO (includi testa per evitare movimenti all'indietro) ===
            var end = eat ? myBodyLength : myBodyLength - 1;
            for (var i = 0; i < end; i++)  // Parti da 0 per includere la testa
            {
                var bodyPart = myBody[i];
                *(ptr + bodyPart.y * width + bodyPart.x) = true;
            }
        }
    }
}