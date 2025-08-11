using System.Runtime.InteropServices;
using Thanos.Enums;
using Thanos.SourceGen;

namespace Thanos.War;

[StructLayout(LayoutKind.Sequential)]
public readonly struct WarContext
{
    public readonly uint Width, Height, Area;

    public readonly uint WarSnakeCount;
    
    private WarContext(uint width, uint height, uint warSnakeCount)
    {
        Width = width;
        Height = height;
        Area = Width * Height;
        
        WarSnakeCount = warSnakeCount;
    }
    
    public WarContext(in Board board) : this(board.Width, board.Height, (uint)board.Snakes.Length)
    {
    }
    
    public static WarContext Worst => new(Constants.MaxWidth, Constants.MaxHeight, Constants.MaxSnakes);
}