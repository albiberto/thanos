using Thanos.Common;
using Thanos.MCST;

namespace Thanos.Memory;

public readonly unsafe struct NodeMemoryLayout
{
    public readonly MemoryBlock Node; 

    private NodeMemoryLayout(int stride) => Node = new MemoryBlock(0, stride);

    public static NodeMemoryLayout Packed => new(sizeof(Node).AlignUp64()); 
}