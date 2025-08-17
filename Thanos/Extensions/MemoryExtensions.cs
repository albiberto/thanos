using System.Runtime.CompilerServices;
using Thanos.Enums;


namespace Thanos.Extensions;

public static class MemoryExtensions
{
    private const long Alignment = Constants.SizeOfCacheLine;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long AlignUp(this long value) => (value + Alignment - 1) & ~(Alignment - 1);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int AlignUp(this int value) => (int)((value + Alignment - 1) & ~(Alignment - 1));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint AlignUp(this uint value) => (uint)((value + Alignment - 1) & ~(Alignment - 1));
}