using System.Runtime.CompilerServices;

namespace Thanos.Common;

public static class MemoryUtils
{
    extension(int value)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int AlignUp8() => value.AlignUp(8);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int AlignUp16() => value.AlignUp(16);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int AlignUp32() => value.AlignUp(32);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int AlignUp64() => value.AlignUp(64);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int AlignUp(int alignment) => (value + alignment - 1) & ~(alignment - 1);
    }
}