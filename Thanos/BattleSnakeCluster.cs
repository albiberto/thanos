using Thanos.Abstract;
using Thanos.Common;
using Thanos.Extensions;
using Thanos.MCST;
using Thanos.Memory;
using Thanos.SourceGen;

namespace Thanos;

public sealed class BattleSnakeCluster : IBattleSnakeCluster, IDisposable
{
    private readonly Engine[] _engines;
    private readonly SlotMemoryPool[] _slotPools;
    private readonly NodeMemoryPool[] _nodePools;
    private readonly LookupsMemoryPool _sharedLookups;

    private readonly ThreadLocal<List<RootMoveStat>> _threadLocalStatsBuffer = new(() => new List<RootMoveStat>(16));
    
    private readonly int[] _lastChosenIndices;
    
    public BattleSnakeCluster(Engine[] engines, SlotMemoryPool[] slotPools, NodeMemoryPool[] nodePools, LookupsMemoryPool sharedLookups)
    {
        if (engines.Length != slotPools.Length || engines.Length != nodePools.Length) throw new ArgumentException("Cluster components length mismatch.");

        _engines = engines;
        _slotPools = slotPools;
        _nodePools = nodePools;
        _sharedLookups = sharedLookups;

        _lastChosenIndices = new int[_engines.Length];
        Array.Fill(_lastChosenIndices, Constants.FirstRootNodeIndex);
    }

    public void InitializeGame(string[] sortedSnakeIds, int count) { }

    public async Task<byte> ComputeMoveAsync(Request request)
    {
        var targetHash = _slotPools[0].CalculateRequestHash(0, in request);

        var tasks = new Task[_engines.Length];
        for (var i = 0; i < _engines.Length; i++)
        {
            var index = i;
            tasks[i] = Task.Run(() => _lastChosenIndices[index] = _engines[index].FindBestMove(in request, _lastChosenIndices[index], targetHash));
        }

        await Task.WhenAll(tasks);

        var totalVisits = new long[5];

        foreach (var engine in _engines)
        {
            var buffer = _threadLocalStatsBuffer.Value!;
            engine.GetRootStats(buffer);

            foreach (var stat in buffer) totalVisits[stat.Move] += stat.Visits;
        }

        var bestMove = Moves.Up;
        long maxVisits = -1;
        ReadOnlySpan<byte> movesToCheck = [Moves.Up, Moves.Down, Moves.Left, Moves.Right];

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

    public void Dispose()
    {
        foreach (var pool in _slotPools) pool.Dispose();
        foreach (var pool in _nodePools) pool.Dispose();
        
        _sharedLookups.Dispose();
        _threadLocalStatsBuffer.Dispose();
    }
}