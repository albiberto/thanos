namespace Snakes.Core.Extensions;

using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

public static class SpanExtensions
{
    public static unsafe int NthIndexOf<T>(this Span<T> span, T value, int n)
        where T : unmanaged
    {
            if (sizeof(T) == sizeof(byte))
            {
                return NthIndexOf(
                    ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)),
                    span.Length,
                    Unsafe.BitCast<T, byte>(value),
                    n);
            }

            throw new NotImplementedException();
    }

    static unsafe int NthIndexOf<T>(this ref T first, int length, T value, int n)
        where T : unmanaged
    {
        Debug.Assert(Vector512.IsHardwareAccelerated && Bmi2.X64.IsSupported && length >= Vector512<T>.Count);

        Vector512<T> current, values = Vector512.Create(value);

        ref var searchSpace = ref first;
        ref var currentSearchSpace = ref searchSpace;
        ref var oneVectorAwayFromEnd = ref Unsafe.Add(ref searchSpace, length - Vector512<T>.Count);

        var missing = n;

        // Loop until either we've finished all elements or there's less than a vector's-worth remaining.
        do
        {
            current = Vector512.LoadUnsafe(ref currentSearchSpace);

            var matches = Vector512.Equals(current, values).ExtractMostSignificantBits();
            var count = BitOperations.PopCount(matches);

            if (count > missing)
            {
                var index = BitOperations.TrailingZeroCount(Bmi2.X64.ParallelBitDeposit(1UL << missing, matches));
                return index + (int)Unsafe.ByteOffset(ref searchSpace, ref currentSearchSpace) / sizeof(T);
            }

            missing -= count;
            currentSearchSpace = ref Unsafe.Add(ref currentSearchSpace, Vector512<T>.Count);
        } while (!Unsafe.IsAddressGreaterThan(ref currentSearchSpace, ref oneVectorAwayFromEnd));

        // If any elements remain, process the last vector in the search space.
        var remaining = length % Vector512<T>.Count;
        if (remaining != 0)
        {
            current = Vector512.LoadUnsafe(ref oneVectorAwayFromEnd);

            var remainingMask = unchecked(~((ulong)-1 >> remaining));

            var matches = Vector512.Equals(current, values).ExtractMostSignificantBits() & remainingMask;
            var count = BitOperations.PopCount(matches);

            if (count > missing)
            {
                var index = BitOperations.TrailingZeroCount(Bmi2.X64.ParallelBitDeposit(1UL << missing, matches));
                return index + (int)Unsafe.ByteOffset(ref searchSpace, ref oneVectorAwayFromEnd) / sizeof(T);
            }
        }

        return -1;
    }
}