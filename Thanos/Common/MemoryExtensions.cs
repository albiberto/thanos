using System.Runtime.CompilerServices;
using Thanos.Memory;

namespace Thanos.Common;

public static class MemoryExtensions
{
    // Questi sono corretti perché 8, 16 e 32 sono potenze di due.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int AlignUp8(this int value) => value.AlignUp(8);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int AlignUp16(this int value) => value.AlignUp(16);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int AlignUp32(this int value) => value.AlignUp(32);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int AlignUpCacheLine(this int value) => value.AlignUp(Constants.CacheLine); // Assumendo sia 64
    
    // Questa logica rimane privata e valida solo per potenze di due.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int AlignUp(this int value, int alignment) => (value + alignment - 1) & ~(alignment - 1);
}