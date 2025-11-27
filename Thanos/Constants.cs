namespace Thanos;

public static class Constants
{
    public const int CacheLine = 64;

    public const int FirstRootNodeIndex = 1;
    
    // Aumentiamo i nodi o riduciamo a seconda della RAM disponibile, 
    // dato che ora ogni nodo pesa il doppio (64 byte vs 32 byte).
    // Con 2.5M nodi * 64 byte = ~160 MB. È accettabile.
    public const int MaxNodes = 2_500_000; 
    public const double TimeoutRatio = .9;

    public const int Large = 19 * 19;
    public const int Medium = 11 * 11;
    public const int Small = 7 * 7;

    public const int MaxSnakesCount = 4; // Fondamentale per il fixed buffer di Node
    public const int EnvironmentPlayerIndex = 255; // ID speciale per il turno "Environment"

    public const int GlobalBitboardsCount = 3;
    public const uint MaxSnakeBodyCapacity = 256;
    public static readonly int[] Areas = [Small, Medium, Large];
}