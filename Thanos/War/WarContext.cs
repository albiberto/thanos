using System.Runtime.InteropServices;
using Thanos.Enums;
using Thanos.SourceGen;

namespace Thanos.War;

[StructLayout(LayoutKind.Sequential)]
public readonly struct WarContext
{
    public readonly uint Width, Height, Area, SnakeCount;

    public static readonly WarContext Worst = new(Constants.MaxWidth, Constants.MaxHeight, Constants.MaxSnakes);

    private WarContext(uint width, uint height, int snakeCount)
    {
        Width = width;
        Height = height;
        Area = width * height;

        SnakeCount = (uint)snakeCount;
    }

    public WarContext(in Board board) : this(board.Width, board.Height, board.Snakes.Length)
    {
    }
}