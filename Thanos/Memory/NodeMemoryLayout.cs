using Thanos.MCST;

namespace Thanos.Memory;

public readonly struct NodeMemoryLayout()
{
    public readonly MemoryBlock Node = MemoryBlock.CreateUp64<Node>(0, 1);
}