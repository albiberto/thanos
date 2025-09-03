using Thanos.Common;

namespace Thanos.War.Snake.Memory;

public readonly unsafe struct WarSnakeMemoryLayout
{
    public readonly int HealthSize;
    public readonly int AnatomySize;
    public readonly int BodySize;

    public readonly int HeaderSize;

    public readonly int Stride;

    public WarSnakeMemoryLayout(int capacity)
    {
        HealthSize = sizeof(Health);
        AnatomySize = sizeof(Anatomy);

        HeaderSize = (HealthSize + AnatomySize).AlignUp();
        BodySize = (sizeof(ushort) * capacity).AlignUp();

        Stride = HeaderSize + BodySize;
    }
}