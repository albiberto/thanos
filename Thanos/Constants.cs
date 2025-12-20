namespace Thanos;

public static class Constants
{
    // Hardware & Memory
    public const int CacheLine = 64;
    
    // Configurazione Bitboard per SIMD
    // Una board 11x11 ha 121 bit.
    // 2 ulong (128 bit) sono perfetti per Vector128<ulong>
    public const int BitboardQuadWords = 2; 

    // Pool Configuration
    public const uint Nodes = 2_000_000;
    public const byte Cores = 4;
    
    // Memory Layout
    public const int FirstIndex = 1; // 0 reserved for null/root
    public const int MaxSnakesCount = 4; // Target per Loop Unrolling
    public const int EnvironmentPlayerIndex = 255;

    // Grid Dimensions
    public static readonly (byte Width, byte Height, ushort Area) Medium = (11, 11, 121);
    
    // Altre dimensioni supportate
    public static readonly (byte Width, byte Height, ushort Area) Small = (7, 7, 49);
    public static readonly (byte Width, byte Height, ushort Area) Large = (19, 19, 361);
}