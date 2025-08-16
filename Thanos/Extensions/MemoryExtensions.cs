using System.Runtime.CompilerServices;
using Thanos.Enums;

namespace Thanos.Extensions;

public static class MemoryExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int AlignUp(this int value, int alignment = Constants.SizeOfCacheLine) => (value + alignment - 1) & ~(alignment - 1);
}