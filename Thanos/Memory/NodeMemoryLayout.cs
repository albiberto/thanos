using Thanos.MCST;

namespace Thanos.Memory;

public unsafe struct NodeMemoryLayout()
{
    public static NodeMemoryLayout Default => new();

    public const int Size = 64;
}
