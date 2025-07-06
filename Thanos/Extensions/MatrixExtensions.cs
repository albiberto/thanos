using System.Runtime.CompilerServices;
using Thanos.Domain;

namespace Thanos.Extensions;

public static class MatrixExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void ClearUnsafe(this bool[,] matrix)
    {
        var length = matrix.Length;

        fixed (bool* ptr = matrix)
        {
            var longPtr = (long*)ptr;
            var longCount = length / 8;

            // Reset 8 bool per volta con loop unrolling
            var i = 0;
            for (; i < longCount - 3; i += 4)
            {
                longPtr[i] = 0;
                longPtr[i + 1] = 0;
                longPtr[i + 2] = 0;
                longPtr[i + 3] = 0;
            }

            // Rimanenti long
            for (; i < longCount; i++) longPtr[i] = 0;

            // Reset byte rimanenti
            for (var j = longCount * 8; j < length; j++) ptr[j] = false;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void FillUnsafe(this bool[,] matrix, Point[] obstacles)
    {
        var lenght = obstacles.Length;
        if (lenght == 0) return;

        fixed (bool* matrixPtr = matrix)
        {
            for (var i = 0; i < lenght; i++)
            {
                var hazard = obstacles[i];
                matrixPtr[hazard.x * 19 + hazard.y] = true;
            }
        }
    }
}