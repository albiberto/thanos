using System.Runtime.InteropServices;
using Thanos.SourceGen;

namespace Thanos.War;

[StructLayout(LayoutKind.Sequential)]
public readonly struct WarContext
{
    public readonly uint Width;
    public readonly uint Area;
    public readonly int Capacity;
    public readonly uint InitialActiveSnakes;
    public readonly uint SnakeStride;
    public readonly uint BitboardsMemorySize;
    public readonly uint SnakesMemorySize;

    public WarContext(in Board board)
    {
        Width = board.Width;
        Area = board.Area;
        Capacity = board.Capacity;
        InitialActiveSnakes = (uint)board.Snakes.Length;
        
        var bitboardSegments = (Area + 63) >> 6;
        BitboardsMemorySize = bitboardSegments * WarField.TotalBitboards * sizeof(ulong);
        
        SnakeStride = (uint)(WarSnake.HeaderSize + Capacity * sizeof(ushort));
        SnakesMemorySize = SnakeStride * InitialActiveSnakes;
    }
}