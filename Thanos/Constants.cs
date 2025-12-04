namespace Thanos;

public static class Constants
{
    public const int CacheLine = 64;
    public const int FirstRootNodeIndex = 1;

    public const uint Nodes = 2_000_000;
    public const byte Cores = 4;

    public static (byte Width, byte Height, ushort Area) Small = (7, 7, 49);
    public static (byte Width, byte Height, ushort Area) Medium = (11, 11, 121);
    public static (byte Width, byte Height, ushort Area) Large = (19, 19, 361);

    public const int MaxSnakesCount = 4; // Fondamentale per il fixed buffer di Node
    public const int EnvironmentPlayerIndex = 255; // ID speciale per il turno "Environment"
}