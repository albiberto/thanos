using Thanos.Abstract;
using Thanos.MCST;
using Thanos.Memory;

namespace Thanos;

public static class Bootstrapper
{
    public static BattleSnakeAgent BuildColdPath(byte firstIndex, byte cores, uint nodes)
    {
        // 1. Risorse Condivise (Read-Only Lookups)
        // Creiamo la lookup table per la griglia 11x11 (Medium)
        var sharedLookups = LookupsMemoryPool.Medium;

        // 2. Definizione Layout di Memoria
        // Definiamo le regole di allineamento e struttura una volta sola
        var nodeLayout = NodeMemoryLayout.Packed; 
        var slotLayout = SlotMemoryLayout.Medium;

        // 3. Allocazione Array
        var nodePools = new NodeMemoryPool[cores];
        var slotPools = new SlotMemoryPool[cores];
        var engines = new Engine[cores];

        // 4. Istanziazione Core (Parallel Setup)
        for (var i = 0; i < cores; i++)
        {
            // Ogni core ha il suo pool di nodi privato (lock-free)
            nodePools[i] = new NodeMemoryPool(nodes, firstIndex, in nodeLayout);
            
            // Ogni core ha il suo pool di slot (stato del gioco)
            slotPools[i] = new SlotMemoryPool(nodes, sharedLookups, in slotLayout);

            // Il motore unisce logica e memoria
            engines[i] = new Engine(slotPools[i], nodePools[i]);
        }

        // 5. Assemblaggio Cluster
        // Il cluster agisce da coordinatore e gestore del ciclo di vita
        IBattleSnakeCluster cluster = new BattleSnakeCluster(engines, slotPools, nodePools, sharedLookups);

        // 6. Creazione Agente (Orchestratore)
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