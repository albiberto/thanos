using System.Runtime.CompilerServices;
using Thanos.Enums;

namespace Thanos.Extensions;

public static class MemoryExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint AlignUp(this uint value, uint alignment = Constants.SizeOfCacheLine) => (value + alignment - 1) & ~(alignment - 1);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint AlignUp(this int value, uint alignment = Constants.SizeOfCacheLine) => ((uint)value).AlignUp(alignment);
}