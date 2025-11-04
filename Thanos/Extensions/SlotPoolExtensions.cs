using Thanos.Common;
using Thanos.Memory;
using Thanos.SourceGen;

namespace Thanos.Extensions;

public static class SlotPoolExtensions
{
    public static long CalculateRequestHash(this SlotMemoryPool slotPool, int index, in Request request)
    {
        var arena = slotPool.GetArena(index);
        arena.InitializeFromRequest(in request);

        var hash = ZobristHasher.CalculateHash(in arena);

        return hash;
    }
}