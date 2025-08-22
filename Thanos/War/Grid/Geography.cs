namespace Thanos.War.Grid;

public readonly struct Geography(int width, int height)
{
    public int Width { get; } = width;
    public int Height { get; } = height;
    public int Area { get; } = width * height;
}