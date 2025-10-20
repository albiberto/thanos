using Thanos.MCST;

namespace Thanos.Memory;

public unsafe struct NodeMemoryLayout()
{
    public static NodeMemoryLayout Default => new();

    public readonly int Size = sizeof(Node);
}