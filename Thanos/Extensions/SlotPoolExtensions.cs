using Thanos.Abstract;
using Thanos.Common;
using Thanos.SourceGen;

namespace Thanos.Extensions;

public static class SlotPoolExtensions
{
    // Ora accetta ISlotPool e gli ID ordinati per il mapping
    public static long CalculateRequestHash(this ISlotMemoryPool slotPool, int index, in Request request, string[] sortedSnakeIds)
    {
        var arena = slotPool.GetGameState(index);
        
        // Passiamo gli ID per mappare correttamente le stringhe JSON agli indici interni (0, 1..)
        arena.InitializeFromRequest(in request, sortedSnakeIds);

        var hash = ZobristHasher.CalculateHash(in arena);

        return hash;
    }
}