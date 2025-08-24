using Thanos.Common;
using Thanos.Memory;

namespace Thanos.MCST.Memory;

public unsafe struct NodeMemoryLayout()
{
    public static NodeMemoryLayout Standard => new();
    
    public readonly int Size = sizeof(Node).AlignUp();
}