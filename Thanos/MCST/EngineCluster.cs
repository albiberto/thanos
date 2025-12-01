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
        Console.WriteLine($"[EngineCluster] Initializing {Constants.CoreCount} engines using {LookupsMemoryLayout.Medium} profile...");

        _engines = new Engine[Constants.CoreCount];
        _slotPools = new SlotMemoryPool[Constants.CoreCount];
        _nodePools = new NodeMemoryPool[Constants.CoreCount];
        _lastChosenIndices = new int[Constants.CoreCount];

        _sharedLookups = new LookupsMemoryPool(LookupsMemoryLayout.Medium); 

        for (var i = 0; i < Constants.CoreCount; i++)
        {
            _nodePools[i] = new NodeMemoryPool(maxNodes, NodeMemoryLayout.Default);
            _slotPools[i] = new SlotMemoryPool(maxNodes, _sharedLookups, SlotMemoryLayout.Medium);
            _engines[i] = new Engine(_slotPools[i], _nodePools[i]);
            
            _lastChosenIndices[i] = Constants.FirstRootNodeIndex;
        }
    }

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
            if (totalVisits[move] <= maxVisits) continue;
            maxVisits = totalVisits[move];
            bestMove = move;
        }
        
        return maxVisits <= 0 
            ? _engines[0].GetFallbackMove() 
            : bestMove;
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