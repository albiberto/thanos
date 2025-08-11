using System.Numerics;
using System.Runtime.InteropServices;
using Thanos.Enums;
using Thanos.SourceGen;

namespace Thanos.War;

[StructLayout(LayoutKind.Sequential)]
public readonly struct WarContext
{
    public readonly uint Width;
    public readonly uint Height;
    public readonly uint Area;
    public readonly int Capacity;
    public readonly uint InitialActiveSnakes;
    public readonly uint SnakeStride;
    public readonly uint BitboardsMemorySize;
    public readonly uint SnakesMemorySize;

    public WarContext(in Board board)
    {
        Width = board.Width;
        Height = board.Height;
        Area = Width * Height;
        Capacity = (int)Math.Min(BitOperations.RoundUpToPowerOf2(Height * Width), Constants.MaxBodyLength);
        InitialActiveSnakes = (uint)board.Snakes.Length;
        
        var bitboardSegments = (Area + 63) >> 6;
        BitboardsMemorySize = bitboardSegments * WarField.TotalBitboards * sizeof(ulong);
        
        SnakeStride = (uint)(WarSnake.HeaderSize + Capacity * sizeof(ushort));
        SnakesMemorySize = SnakeStride * InitialActiveSnakes;
    }
}