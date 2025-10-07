using Thanos.Common;
using Thanos.Memory;
using Thanos.SourceGen;

namespace Thanos.Extensions;

public static class SlotPoolExtensions
{
    public static long CalculateHash(this SlotMemoryPool slotPool, int index, in Request request)
    {
        var arena = slotPool.GetArena(index);
        arena.InitializeFromRequest(in request);

        var hash = ZobristHasher.CalculateHash(in arena);
        
        #if DEBUG
            Console.WriteLine($"[SlotPoolExtensions] Request hash computed from slot index {index}: {hash}");
        #endif
        
        return hash;
    }
}