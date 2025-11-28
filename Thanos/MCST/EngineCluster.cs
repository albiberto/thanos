using System.Diagnostics;
using Thanos.Common;
using Thanos.Extensions;
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
        // Configurazione Core (Manuale per Debug o Automatica)
        const int coreCount = 1; 
        
        Console.WriteLine($"[EngineCluster] Initializing {coreCount} engines using 'Medium' (11x11) profile...");

        _engines = new Engine[coreCount];
        _slotPools = new SlotMemoryPool[coreCount];
        _nodePools = new NodeMemoryPool[coreCount];
        _lastChosenIndices = new int[coreCount];

        // 1. LOOKUPS: Profilo Medium (11x11)
        _sharedLookups = new LookupsMemoryPool(LookupsMemoryLayout.Medium); 

        for (var i = 0; i < coreCount; i++)
        {
            _nodePools[i] = new NodeMemoryPool(maxNodes, NodeMemoryLayout.Default);
            
            // 2. SLOTS: Profilo Medium (11x11)
            // Perfettamente allineato con LookupsMemoryLayout.Medium
            _slotPools[i] = new SlotMemoryPool(maxNodes, _sharedLookups, SlotMemoryLayout.Medium);
            
            _engines[i] = new Engine(_slotPools[i], _nodePools[i]);
            _lastChosenIndices[i] = Constants.FirstRootNodeIndex;
        }
    }

    // ... (Il resto della classe: ComputeMoveAsync, Reset, Dispose rimane invariato) ...
    public async Task<byte> ComputeMoveAsync(Request request)
    {
        var targetHash = _slotPools[0].CalculateRequestHash(0, in request);

        var tasks = new Task[_engines.Length];
        for (var i = 0; i < _engines.Length; i++)
        {
            var index = i;
            tasks[i] = Task.Run(() =>
            {
                var bestLocalIndex = _engines[index].FindBestMove(in request, _lastChosenIndices[index], targetHash);
                _lastChosenIndices[index] = bestLocalIndex;
            });
        }

        await Task.WhenAll(tasks);

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