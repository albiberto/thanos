using System.Runtime.InteropServices;
using Thanos.Enums;
using Thanos.SourceGen;

namespace Thanos.War;

[StructLayout(LayoutKind.Sequential)]
public readonly struct WarContext
{
    public readonly int Width, Height, Area, SnakeCount;

    public static readonly WarContext Worst = new(Constants.MaxWidth, Constants.MaxHeight, Constants.MaxSnakes);

    private WarContext(int width, int height, int snakeCount)
    {
        Width = width;
        Height = height;
        Area = width * height;

        SnakeCount = snakeCount;
    }

    public WarContext(in Board board) : this(board.Width, board.Height, board.Snakes.Length)
    {
    }
}