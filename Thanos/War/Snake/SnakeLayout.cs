using Thanos.Memory;

namespace Thanos.War.Snake;

public readonly unsafe struct SnakeLayout
{
    public readonly int ProfileSize;
    public readonly int HealthSize;
    public readonly int AnatomySize;
    public readonly int BodySize;
        
    public readonly int HeaderSize;
    
    public readonly int Stride;

    public SnakeLayout(int capacity)
    {
        ProfileSize = sizeof(Profile);
        HealthSize = sizeof(Health);
        AnatomySize = sizeof(Anatomy);
        
        HeaderSize = (ProfileSize + HealthSize + AnatomySize).AlignUp();
        BodySize = (sizeof(ushort) * capacity).AlignUp();
        
        Stride = HeaderSize + BodySize;
    }
}