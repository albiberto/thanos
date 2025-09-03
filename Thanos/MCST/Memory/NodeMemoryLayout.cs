namespace Thanos.MCST.Memory;

public unsafe struct NodeMemoryLayout()
{
    public static NodeMemoryLayout Instance => new();

    public readonly int Size = sizeof(Node);
}