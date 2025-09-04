using Thanos.Common;

namespace Thanos.War.Snake.Memory;

public readonly unsafe struct WarSnakeMemoryLayout(int bodyCapacity)
{
    public readonly int HeaderStride = (sizeof(Health) + sizeof(Anatomy)).AlignUp8();

    public readonly int BodyCapacity = bodyCapacity;
    public readonly int BodySize = sizeof(ushort) * bodyCapacity;
}