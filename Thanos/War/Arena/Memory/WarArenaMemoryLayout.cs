using Thanos.Memory;

namespace Thanos.War.Arena.Memory;

public readonly unsafe struct WarArenaMemoryLayout()
{
    public readonly int Header = sizeof(WarArenaHeader).AlignUp();
}