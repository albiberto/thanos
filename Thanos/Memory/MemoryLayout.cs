using System.Runtime.InteropServices;
using Thanos.Common;
using Thanos.MCST;
using Thanos.MCST.Memory;
using Thanos.War.Grid.Memory;
using Thanos.War.Snake.Memory;

namespace Thanos.Memory;

[StructLayout(LayoutKind.Sequential)]
public readonly struct MemoryLayout
{
    public const int SizeOfCacheLine = 64;
    
    public readonly NodeMemoryLayout Node;
    public readonly WarGridMemoryLayout WarGrid;
    public readonly WarSnakeMemoryLayout WarSnake;

    public readonly int WarSlotSize;

    public readonly Offsets Offsets;
    
    public MemoryLayout(int capacity, int area, int snakeCount, int neighborsLenght)
    {
        Node = new();
        WarGrid = new(area, neighborsLenght);
        WarSnake = new(capacity);

        WarSlotSize = (WarGrid.Size + WarSnake.Stride * snakeCount).AlignUp();
        
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