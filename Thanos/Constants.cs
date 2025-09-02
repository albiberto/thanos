namespace Thanos;

public static class Constants
{
    public const int CacheLine = 64;

    public const int MaxNodes = 2_000_000;
    public const double TimeoutRatio = .9;

    public const int MaxWidth = 19;
    public const int MaxHeight = 19;
    public const int MaxArea = MaxWidth * MaxHeight;

    public const int MaxSnakesCount = 8;
    public const uint MaxSnakeBodyCapacity = 256;
}