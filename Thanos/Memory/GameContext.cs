using System.Numerics;

namespace Thanos.Memory;

public struct GameContext
{
    public readonly int Width;
    public readonly int Height;
    public readonly int Area;

    public readonly Dictionary<string, int> SnakeIdMap = new(StringComparer.InvariantCultureIgnoreCase);
    public readonly int SnakesCount;
    public readonly int Capacity;

    public static GameContext Worst() => new(Constants.MaxWidth, Constants.MaxSnakesCount);

    private GameContext(int width, int snakesCount)
    {
        Width = Height = width;
        Area = width * width;

        SnakesCount = snakesCount;
        Capacity = (int)Math.Min(BitOperations.RoundUpToPowerOf2((uint)Area), Constants.MaxSnakeBodyCapacity);
    }

    public GameContext(int width, Dictionary<string, int> snakeIdMap) : this(width, snakeIdMap.Count) => SnakeIdMap = snakeIdMap;
}