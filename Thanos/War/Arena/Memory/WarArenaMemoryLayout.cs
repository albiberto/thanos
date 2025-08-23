using Thanos.Memory;
using Thanos.War.Grid;

namespace Thanos.War.Arena.Memory;

public readonly unsafe struct WarArenaMemoryLayout()
{
    public readonly int Header = sizeof(WarArenaHeader).AlignUp();
    public readonly int Moves = sizeof(Geography).AlignUp();
}