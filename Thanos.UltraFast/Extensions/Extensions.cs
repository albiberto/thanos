using System.Runtime.CompilerServices;

namespace Thanos.UltraFast.Extensions;

/// <summary>
/// Helper per conversioni coordinate ultra-veloci
/// </summary>
public static class Extensions
{
    /// <summary>Dimensione cache line CPU moderna</summary>
    public const int CACHE_LINE_SIZE = 64;
    
    /// <summary>Dimensione massima griglia supportata</summary>
    public const int MAX_SIZE = 100;
    
    /// <summary>Valore sentinella per posizione invalida</summary>
    public const ushort INVALID = 65535;
    
    // Inline aggressivo per tutti i calcoli matematici
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort ToIndex(int x, int y, int width) => (ushort)(y * width + x);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static (byte x, byte y) ToCoords(ushort index, int width) => ((byte)(index % width), (byte)(index / width));
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsValidPosition(int x, int y, int width, int height) => ((uint)x < (uint)width) & ((uint)y < (uint)height);
}