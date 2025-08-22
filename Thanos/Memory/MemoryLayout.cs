using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Thanos.MCST;
using Thanos.War.Grid;
using Thanos.War.Snake;

namespace Thanos.Memory;

[StructLayout(LayoutKind.Sequential)]
public readonly struct MemoryLayout
{
    public const int SizeOfCacheLine = 64;
    
    public readonly MonteCarloLayout Node = new();
    public readonly GridLayout Grid = new();
    public readonly SnakeLayout Snake;

    public readonly Offsets Offsets = new();
    
    public MemoryLayout(int capacity, int area)
    {
        Node = new();
        Grid = new(area);
        Snake = new(capacity);

        Offsets = new(Node.Size, Grid.Size);
    }
}

public readonly struct Offsets(int sizeOfNode, int sizeOfGrid)
{
    public const int Node = 0;
    
    public readonly int Grid = sizeOfNode;
    public readonly int Snakes = sizeOfNode + sizeOfGrid;
}

public static class MemoryExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int AlignUp(this int value) => (value + MemoryLayout.SizeOfCacheLine - 1) & ~(MemoryLayout.SizeOfCacheLine - 1);
}