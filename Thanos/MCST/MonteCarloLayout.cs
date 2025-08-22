using Thanos.Memory;

namespace Thanos.MCST;

public readonly unsafe struct MonteCarloLayout()
{
    public readonly int Size = sizeof(Node).AlignUp();
}