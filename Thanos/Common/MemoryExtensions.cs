using System.Runtime.CompilerServices;
using Thanos.Memory;

namespace Thanos.Common;

public static class MemoryExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int AlignUp(this int value) => (value + MemoryLayout.SizeOfCacheLine - 1) & ~(MemoryLayout.SizeOfCacheLine - 1);
}