using Thanos.MCST;

namespace Thanos.Memory;

public readonly unsafe struct NodeMemoryLayout(int size)
{
    public static NodeMemoryLayout Default => new(sizeof(Node)); 

    public readonly int Size = size;
}