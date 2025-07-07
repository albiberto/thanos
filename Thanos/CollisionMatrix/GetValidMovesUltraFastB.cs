using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Thanos.Domain;
using Thanos.Enums;
using Thanos.Model;

namespace Thanos.CollisionMatrix;

// === VERSIONE ULTRAFAST OTTIMIZZATA ===
// Benchmarking ha mostrato che per board grandi (19x19) e con 4 serpenti il tempo di esecuzione è ~20ns,
public static class GetValidMovesUltraFastB
{
    private static readonly byte[] _collisionBytes = new byte[19 * 19]; // bool[,] → byte[] più veloce
    private static readonly Direction[] _validMoves = new Direction[4];
    private const int UP = 0, DOWN = 1, LEFT = 2, RIGHT = 3;
    
    public static unsafe int GetValidMovesUltraFast(uint width, uint height, string myId, Point[] myBody, int myBodyLength, uint myHeadX, uint myHeadY, Point[] hazards, int hazardCount, Snake[] snakes, int snakeCount, bool eat)
    {
        ClearCollisionMatrix();
        BuildCollisionMatrix(width, height, myId, myBody, myBodyLength, myHeadX, myHeadY, hazards, hazardCount, snakes,snakeCount, eat);
        
        return GetValidMovesFromMatrix(width, height, myHeadX, myHeadY);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void ClearCollisionMatrix()
    {
        fixed (byte* ptr = _collisionBytes)
        {
            // SIMD clearing - 32 byte per volta con AVX2
            if (Avx2.IsSupported)
            {
                var vectorCount = _collisionBytes.Length / 32;
                var zero = Vector256<byte>.Zero;
                
                for (var i = 0; i < vectorCount; i++)
                    Avx.Store(ptr + i * 32, zero);
                
                // Rimanenti byte
                var remaining = _collisionBytes.Length % 32;
                if (remaining > 0)
                    Unsafe.InitBlock(ptr + vectorCount * 32, 0, (uint)remaining);
            }
            else
            {
                // Fallback con long (8 byte per volta)
                var longPtr = (long*)ptr;
                var longCount = _collisionBytes.Length / 8;
                
                // Loop unrolling aggressivo
                var i = 0;
                for (; i + 8 <= longCount; i += 8)
                {
                    longPtr[i] = 0;
                    longPtr[i + 1] = 0;
                    longPtr[i + 2] = 0;
                    longPtr[i + 3] = 0;
                    longPtr[i + 4] = 0;
                    longPtr[i + 5] = 0;
                    longPtr[i + 6] = 0;
                    longPtr[i + 7] = 0;
                }
                
                for (; i < longCount; i++)
                    longPtr[i] = 0;
            }
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void BuildCollisionMatrix(uint width, uint height, string myId, Point[] myBody, int myBodyLength, uint myHeadX, uint myHeadY, Point[] hazards, int hazardCount, Snake[] snakes, int snakeCount, bool eat)
    {
        fixed (byte* ptr = _collisionBytes)
        {
            var widthInt = (int)width;
            
            // === HAZARDS - No bounds checking ===
            for (var i = 0; i < hazardCount; i++)
            {
                ref var hazard = ref hazards[i];
                *(ptr + hazard.y * widthInt + hazard.x) = 1;
            }
            
            // === SERPENTI NEMICI - Batch processing ===
            for (var i = 0; i < snakeCount; i++)
            {
                ref var snake = ref snakes[i];
                if (ReferenceEquals(snake.id, myId)) continue;
                
                var enemyBody = snake.body;
                var enemyBodyLength = enemyBody.Length;
                
                // Corpo nemico - loop unrolling
                var j = 0;
                for (; j + 4 <= enemyBodyLength; j += 4)
                {
                    ref var bp1 = ref enemyBody[j];
                    ref var bp2 = ref enemyBody[j + 1];
                    ref var bp3 = ref enemyBody[j + 2];
                    ref var bp4 = ref enemyBody[j + 3];
                    
                    *(ptr + bp1.y * widthInt + bp1.x) = 1;
                    *(ptr + bp2.y * widthInt + bp2.x) = 1;
                    *(ptr + bp3.y * widthInt + bp3.x) = 1;
                    *(ptr + bp4.y * widthInt + bp4.x) = 1;
                }
                
                for (; j < enemyBodyLength; j++)
                {
                    ref var bp = ref enemyBody[j];
                    *(ptr + bp.y * widthInt + bp.x) = 1;
                }
                
                // HEAD-TO-HEAD - Branchless
                if (enemyBodyLength > myBodyLength)
                {
                    var enemyHead = snake.head;
                    var hx = enemyHead.x;
                    var hy = enemyHead.y;
                    var offset = hy * widthInt + hx;
                    
                    // Usa conditional move invece di branches
                    *(ptr + offset - 1) = (byte)(hx > 0 ? 1 : *(ptr + offset - 1));
                    *(ptr + offset + 1) = (byte)(hx < width - 1 ? 1 : *(ptr + offset + 1));
                    *(ptr + offset - widthInt) = (byte)(hy > 0 ? 1 : *(ptr + offset - widthInt));
                    *(ptr + offset + widthInt) = (byte)(hy < height - 1 ? 1 : *(ptr + offset + widthInt));
                }
            }
            
            // === MIO CORPO - Batch processing ===
            var end = eat ? myBodyLength : myBodyLength - 1;
            var k = 0;
            for (; k + 4 <= end; k += 4)
            {
                ref var bp1 = ref myBody[k];
                ref var bp2 = ref myBody[k + 1];
                ref var bp3 = ref myBody[k + 2];
                ref var bp4 = ref myBody[k + 3];
                
                *(ptr + bp1.y * widthInt + bp1.x) = 1;
                *(ptr + bp2.y * widthInt + bp2.x) = 1;
                *(ptr + bp3.y * widthInt + bp3.x) = 1;
                *(ptr + bp4.y * widthInt + bp4.x) = 1;
            }
            
            for (; k < end; k++)
            {
                ref var bp = ref myBody[k];
                *(ptr + bp.y * widthInt + bp.x) = 1;
            }
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe int GetValidMovesFromMatrix(uint width, uint height, uint myHeadX, uint myHeadY)
    {
        var count = 0;
        var widthInt = (int)width;
        
        fixed (byte* ptr = _collisionBytes)
        {
            var baseOffset = (int)myHeadY * widthInt + (int)myHeadX;
            
            // Branchless validation - usa conditional moves
            var canUp = myHeadY > 0 && *(ptr + baseOffset - widthInt) == 0;
            var canDown = myHeadY < height - 1 && *(ptr + baseOffset + widthInt) == 0;
            var canLeft = myHeadX > 0 && *(ptr + baseOffset - 1) == 0;
            var canRight = myHeadX < width - 1 && *(ptr + baseOffset + 1) == 0;
            
            // Branchless assignment
            _validMoves[count] = UP;
            count += canUp ? 1 : 0;
            
            _validMoves[count] = (Direction)DOWN;
            count += canDown ? 1 : 0;
            
            _validMoves[count] = (Direction)LEFT;
            count += canLeft ? 1 : 0;
            
            _validMoves[count] = (Direction)RIGHT;
            count += canRight ? 1 : 0;
        }
        
        return count;
    }
}