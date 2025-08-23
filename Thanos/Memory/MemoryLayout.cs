using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Thanos.MCST;
using Thanos.War.Arena.Memory;
using Thanos.War.Grid.Memory;
using Thanos.War.Snake.Memory;

namespace Thanos.Memory;

[StructLayout(LayoutKind.Sequential)]
public readonly struct MemoryLayout
{
    public const int SizeOfCacheLine = 64;
    
    public readonly MonteCarloLayout Node;
    public readonly WarGridMemoryLayout Grid;
    public readonly WarSnakeMemoryLayout Snake;
    public readonly WarArenaMemoryLayout Arena;

    public readonly Offsets Offsets;
    
    public MemoryLayout(int capacity, int area)
    {
        Node = new();
        Grid = new(area);
        Snake = new(capacity);
        Arena = new();

        Offsets = new(Node.Size, Grid.Size);
    }
}

public readonly struct Offsets(int sizeOfNode, int sizeOfGrid)
{
    public readonly int Node = 0;
    
    public readonly int Grid = sizeOfNode;
    public readonly int Snakes = sizeOfNode + sizeOfGrid;
    public readonly int Arena = sizeOfNode + sizeOfGrid;
}

public static class MemoryExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int AlignUp(this int value) => (value + MemoryLayout.SizeOfCacheLine - 1) & ~(MemoryLayout.SizeOfCacheLine - 1);
}