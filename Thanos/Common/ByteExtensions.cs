using System.Numerics;
using System.Runtime.CompilerServices;

namespace Thanos.Common;

public static class ByteExtensions
{
    /// <summary>
    ///     Calcola il numero di zeri consecutivi a partire dal bit meno significativo.
    ///     Esempio: 1 (0001) -> 0; 2 (0010) -> 1; 4 (0100) -> 2; 8 (1000) -> 3.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int NumberOfTrailingZeros(this byte b) => BitOperations.TrailingZeroCount(b);
}