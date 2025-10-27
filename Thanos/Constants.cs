namespace Thanos;

public static class Constants
{
    public const int CacheLine = 64;

    public const int MaxNodes = 2_500_000;
    public const double TimeoutRatio = .9;

    public const int Large = 19 * 19;
    public const int Medium = 11 * 11;
    public const int Small = 7 * 7;

    public const int MaxSnakesCount = 4;
    public const int GlobalBitboardsCount = 3;
    public const uint MaxSnakeBodyCapacity = 256;
    public static readonly int[] Areas = [Small, Medium, Large];
}