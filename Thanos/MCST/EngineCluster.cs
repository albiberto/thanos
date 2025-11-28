using System.Diagnostics;
using Thanos.Common;
using Thanos.Memory;
using Thanos.SourceGen;

namespace Thanos.MCST;

public sealed class EngineCluster : IDisposable
{
    private readonly Engine[] _engines;
    private readonly SlotMemoryPool[] _slotPools;
    private readonly NodeMemoryPool[] _nodePools;
    
    private readonly LookupsMemoryPool _sharedLookups; 
    
    private readonly int[] _lastChosenIndices;

    private readonly ThreadLocal<List<RootMoveStat>> _threadLocalStatsBuffer = new(() => new List<RootMoveStat>(16));

    public EngineCluster(uint maxNodes)
    {
        // MODIFICA QUI: Imposta manualmente i core o limitali
        // var coreCount = Math.Max(1, Environment.ProcessorCount); // <-- Vecchia logica (Tutti i core)
        
        const int coreCount = 1; // <-- NUOVA LOGICA: Forza 2 Core per il test
        
        Console.WriteLine($"[EngineCluster] Initializing {coreCount} engines (Manual Limit)...");

        _engines = new Engine[coreCount];
        _slotPools = new SlotMemoryPool[coreCount];
        _nodePools = new NodeMemoryPool[coreCount];
        _lastChosenIndices = new int[coreCount];

        _sharedLookups = new LookupsMemoryPool(LookupsMemoryLayout.Large);

        for (var i = 0; i < coreCount; i++)
        {
            _nodePools[i] = new NodeMemoryPool(maxNodes, NodeMemoryLayout.Default);
            _slotPools[i] = new SlotMemoryPool(maxNodes, _sharedLookups, SlotMemoryLayout.Worst);
            _engines[i] = new Engine(_slotPools[i], _nodePools[i]);
            _lastChosenIndices[i] = Constants.FirstRootNodeIndex;
        }
    }

    // ... Il resto della classe rimane identico ...
    // (Incluso ComputeMoveAsync, Reset, SetMap, Dispose)
    
    // Metodo Async standard (senza unsafe)
    public async Task<byte> ComputeMoveAsync(Request request)
    {
        // 1. Esecuzione Parallela
        var tasks = new Task[_engines.Length];
        for (var i = 0; i < _engines.Length; i++)
        {
            var index = i; // Capture index
            tasks[i] = Task.Run(() =>
            {
                // Avvolgiamo la chiamata all'Engine (che manipola puntatori) in un blocco unsafe
                var bestLocalIndex = _engines[index].FindBestMove(in request, _lastChosenIndices[index]);
                _lastChosenIndices[index] = bestLocalIndex;
            });
        }

        await Task.WhenAll(tasks);

        // 2. Merge dei Risultati
        var totalVisits = new long[16]; 

        foreach (var engine in _engines)
        {
            var buffer = _threadLocalStatsBuffer.Value!;

            engine.GetRootStats(buffer);

            foreach (var stat in buffer)
            {
                totalVisits[stat.Move] += stat.Visits;
            }
        }

        // 3. Selezione Finale
        var bestMove = Moves.Up; 
        long maxVisits = -1;

        byte[] movesToCheck = [Moves.Up, Moves.Down, Moves.Left, Moves.Right];
        
        foreach (var move in movesToCheck)
        {
            if (totalVisits[move] > maxVisits)
            {
                maxVisits = totalVisits[move];
                bestMove = move;
            }
        }
        
        if (maxVisits <= 0) return Moves.None;

        return bestMove;
    }

    public void Reset()
    {
        for (var i = 0; i < _engines.Length; i++)
        {
            _engines[i].Reset();
            _lastChosenIndices[i] = Constants.FirstRootNodeIndex;
        }
    }
    
    public void SetMap(Dictionary<string, int> map)
    {
        foreach (var pool in _slotPools) pool.Set(map);
    }

    public void Dispose()
    {
        foreach (var pool in _slotPools) pool.Dispose();
        foreach (var pool in _nodePools) pool.Dispose();
        _sharedLookups.Dispose();
        _threadLocalStatsBuffer.Dispose();
    }
}