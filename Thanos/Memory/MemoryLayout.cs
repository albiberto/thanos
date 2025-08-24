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
    public readonly WarGridMemoryLayout WarGrid;
    public readonly WarSnakeMemoryLayout WarSnake;
    public readonly WarArenaMemoryLayout WarArena;

    public readonly int SlotSize;

    public readonly Offsets Offsets;
    
    public MemoryLayout(int capacity, int area, int snakeCount, int neighborsLenght)
    {
        Node = new();
        WarGrid = new(area, neighborsLenght);
        WarSnake = new(capacity);
        WarArena = new();

        SlotSize = (Node.Size + WarGrid.Size + WarSnake.Stride * snakeCount + WarArena.Size).AlignUp();
        
        Offsets = new(Node.Size, WarGrid.Size);
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