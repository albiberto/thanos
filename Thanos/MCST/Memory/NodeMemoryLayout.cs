using Thanos.Common;

namespace Thanos.MCST.Memory;

public unsafe struct NodeMemoryLayout()
{
    public static NodeMemoryLayout Standard => new();

    public readonly int Size = sizeof(Node).AlignUp();
}