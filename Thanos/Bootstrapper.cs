using Thanos.Abstract;
using Thanos.MCST;
using Thanos.Memory;

namespace Thanos;

public static class Bootstrapper
{
    public static BattleSnakeAgent BuildColdPath(byte firstIndex, byte cores, uint nodes)
    {
        // 1. Risorse Condivise
        var sharedLookups = LookupsMemoryPool.Medium;

        // 2. Layout Memoria
        var nodeLayout = new NodeMemoryLayout();
        var slotLayout = new SlotMemoryLayout(Constants.Medium.Area, 64, Constants.MaxSnakesCount);

        // 3. Allocazione Shared Memory Pools (HIVE MIND)
        // Capacità totale = nodi per core * numero core
        var totalNodesCapacity = nodes * cores; 
        
        var sharedNodePool = new NodeMemoryPool(totalNodesCapacity, firstIndex, in nodeLayout);
        var sharedSlotPool = new SlotMemoryPool(totalNodesCapacity, firstIndex, Constants.MaxSnakesCount, sharedLookups, in slotLayout);

        // 4. Istanziazione Workers (consumano gli stessi pool)
        var workers = new IWorker[cores];
        for (var i = 0; i < cores; i++)
        {
            workers[i] = new Worker(sharedSlotPool, sharedNodePool);
        }

        // 5. Assemblaggio Engine Unico
        var engine = new Engine(sharedSlotPool, sharedNodePool, workers);

        // 6. Creazione Agente (Cluster eliminato)
        return new(engine, sharedSlotPool, sharedNodePool, sharedLookups);
    }

    public static void OverrideConsoleStandardOutput()
    {
#if DEBUG
        var logFileStream = new FileStream("log.log", FileMode.Create, FileAccess.ReadWrite);
        var logStreamWriter = new StreamWriter(logFileStream) { AutoFlush = true };
        Console.SetOut(logStreamWriter);
        Console.SetError(logStreamWriter); 
#endif
    }
}