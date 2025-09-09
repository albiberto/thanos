namespace Thanos;

public static class Constants
{
    public const int CacheLine = 64;

    public const int MaxNodes = 20_000_000;
    public const double TimeoutRatio = .9;

    public const int Large = 19 * 19;
    public const int Medium = 11 * 11;
    public const int Small = 7 * 7;

    public const int MaxSnakesCount = 8;
    public const uint MaxSnakeBodyCapacity = 256;
    public static readonly int[] Areas = [Small, Medium, Large];
}