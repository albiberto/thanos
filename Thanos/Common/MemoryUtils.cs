using System.Runtime.CompilerServices;
using Thanos.Memory;

namespace Thanos.Common;

public static class MemoryUtils
{
    // Questi sono corretti perché 8, 16 e 32 sono potenze di due.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int AlignUp8(this int value) => value.AlignUp(8);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int AlignUp16(this int value) => value.AlignUp(16);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int AlignUp32(this int value) => value.AlignUp(32);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int AlignUp64(this int value) => value.AlignUp(Constants.CacheLine); // Assumendo sia 64
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int AlignUp(this int value, int alignment) => (value + alignment - 1) & ~(alignment - 1);
}