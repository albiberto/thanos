using Thanos.Abstract;
using Thanos.MCST;
using Thanos.Memory;

namespace Thanos;

public static class Bootstrapper
{
    public static BattleSnakeAgent BuildColdPath(byte firstIndex, byte cores, uint nodes)
    {
        var sharedLookups = LookupsMemoryPool.Medium;

        var nodePools = new NodeMemoryPool[cores];
        var slotPools = new SlotMemoryPool[cores];

        var engines = new Engine[cores];

        for (var i = 0; i < cores; i++)
        {
            var nodeMemoryLayout = NodeMemoryLayout.Default;
            nodePools[i] = new NodeMemoryPool(nodes, firstIndex, in nodeMemoryLayout);
            
            slotPools[i] = new SlotMemoryPool(nodes, sharedLookups, SlotMemoryLayout.Medium);

            engines[i] = new Engine(slotPools[i], nodePools[i]);
        }

        IBattleSnakeCluster cluster = new BattleSnakeCluster(engines, slotPools, nodePools, sharedLookups);

        return new BattleSnakeAgent(cluster);
    }

    public static void OverrideConsoleStandardOutput()
    {
#if !DEBUG
        return;
#endif

        var logFileStream = new FileStream("log.log", FileMode.Create, FileAccess.ReadWrite);
        var logStreamWriter = new StreamWriter(logFileStream) { AutoFlush = true };
        Console.SetOut(logStreamWriter);
        Console.SetError(logStreamWriter);
    }
}