using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Thanos.Domain;
using Thanos.Enums;
using Thanos.Model;
using Thanos.Extensions;

namespace Thanos.CollisionMatrix;

public static class GetValidMovesLightSpeedB
{
    private static readonly byte[] _collisionBytes = new byte[19 * 19];
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetValidMovesLightSpeed(uint width, uint height, string myId, Point[] myBody, int myBodyLength, uint myHeadX, uint myHeadY, Point[] hazards, int hazardCount, Snake[] snakes, int snakeCount, bool eat)
    {
        ClearCollisionMatrix();
        BuildCollisionMatrix(width, height, myId, myBody, myBodyLength, myHeadX, myHeadY, hazards, hazardCount, snakes, snakeCount, eat);
        
        return GetValidMovesFromMatrix(width, height, myHeadX, myHeadY);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void ClearCollisionMatrix()
    {
        fixed (byte* ptr = _collisionBytes)
        {
            if (Avx2.IsSupported)
            {
                var vectorCount = _collisionBytes.Length / 32;
                var zero = Vector256<byte>.Zero;
                
                for (var i = 0; i < vectorCount; i++)
                    Avx.Store(ptr + i * 32, zero);
                
                var remaining = _collisionBytes.Length % 32;
                if (remaining > 0)
                    Unsafe.InitBlock(ptr + vectorCount * 32, 0, (uint)remaining);
            }
            else
            {
                var longPtr = (long*)ptr;
                var longCount = _collisionBytes.Length / 8;
                
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
            
            // HAZARDS
            for (var i = 0; i < hazardCount; i++)
            {
                ref var hazard = ref hazards[i];
                *(ptr + hazard.y * widthInt + hazard.x) = 1;
            }
            
            // SERPENTI NEMICI
            for (var i = 0; i < snakeCount; i++)
            {
                ref var snake = ref snakes[i];
                if (ReferenceEquals(snake.id, myId)) continue;
                
                var enemyBody = snake.body;
                var enemyBodyLength = enemyBody.Length;
                
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
                
                // HEAD-TO-HEAD
                if (enemyBodyLength > myBodyLength)
                {
                    var enemyHead = snake.head;
                    var hx = enemyHead.x;
                    var hy = enemyHead.y;
                    var offset = hy * widthInt + hx;
                    
                    *(ptr + offset - 1) = (byte)(hx > 0 ? 1 : *(ptr + offset - 1));
                    *(ptr + offset + 1) = (byte)(hx < width - 1 ? 1 : *(ptr + offset + 1));
                    *(ptr + offset - widthInt) = (byte)(hy > 0 ? 1 : *(ptr + offset - widthInt));
                    *(ptr + offset + widthInt) = (byte)(hy < height - 1 ? 1 : *(ptr + offset + widthInt));
                }
            }
            
            // MIO CORPO
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
        // Debug.Print(width, height, _collisionBytes);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe int GetValidMovesFromMatrix(uint width, uint height, uint myHeadX, uint myHeadY)
    {
        var widthInt = (int)width;
        
        fixed (byte* ptr = _collisionBytes)
        {
            var baseOffset = (int)myHeadY * widthInt + (int)myHeadX;
            
            // Branchless validation con bit manipulation
            var result = 0;
            
            // UP: y > 0 && matrix[y-1][x] == 0
            result |= myHeadY > 0 && *(ptr + baseOffset - widthInt) == 0 ? MonteCarlo.UP : 0;
            
            // DOWN: y < height-1 && matrix[y+1][x] == 0  
            result |= myHeadY < height - 1 && *(ptr + baseOffset + widthInt) == 0 ? MonteCarlo.DOWN : 0;
            
            // LEFT: x > 0 && matrix[y][x-1] == 0
            result |= myHeadX > 0 && *(ptr + baseOffset - 1) == 0 ? MonteCarlo.LEFT : 0;
            
            // RIGHT: x < width-1 && matrix[y][x+1] == 0
            result |= myHeadX < width - 1 && *(ptr + baseOffset + 1) == 0 ? MonteCarlo.RIGHT : 0;
            
            return result;
        }
    }
    
    // METODI INTERRESSATI PER IL FUTURO MA CHE PREFERIESCO NON UTILIZZARE PERCHE' FANNO CAST E VORREI LAVORARE DIRETTAMENTE CON I BIT
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetMoveCount(int validMoves)
    {
        // Conta i bit settati usando bit manipulation
        var count = validMoves;
        count -= (count >> 1) & 0x55555555;
        count = (count & 0x33333333) + ((count >> 2) & 0x33333333);
        return ((count + (count >> 4)) & 0x0F0F0F0F) * 0x01010101 >> 24;
    }
    
    // Metodo per convertire in array di Direction (se necessario)
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Direction[] ToDirectionArray(int validMoves)
    {
        var directions = new Direction[GetMoveCount(validMoves)];
        var index = 0;
        
        if ((validMoves & MonteCarlo.UP) != 0) directions[index++] = Direction.Up;
        if ((validMoves & MonteCarlo.DOWN) != 0) directions[index++] = Direction.Down;
        if ((validMoves & MonteCarlo.LEFT) != 0) directions[index++] = Direction.Left;
        if ((validMoves & MonteCarlo.RIGHT) != 0) directions[index] = Direction.Right;
        
        return directions;
    }
}