using System.Runtime.CompilerServices;

namespace Thanos.Extensions;

public static class MemoryExtensions
{
    // Funzione helper per arrotondare un valore al multiplo superiore dell'allineamento.
    // Esempio: AlignUp(13) -> 16. AlignUp(16) -> 16.
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint AlignUp(this uint value, uint alignment = 8) => (value + alignment - 1) & ~(alignment - 1);
    
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint AlignUp(this int value, uint alignment = 8) => ((uint)value).AlignUp(alignment);
}