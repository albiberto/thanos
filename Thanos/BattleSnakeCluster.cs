using Thanos.Abstract;
using Thanos.Common;
using Thanos.Extensions; // Namespace del tuo extension method
using Thanos.MCST;
using Thanos.Memory;
using Thanos.SourceGen;

namespace Thanos;

public sealed class BattleSnakeCluster : IBattleSnakeCluster
{
    private readonly Engine[] _engines;
    private readonly ISlotMemoryPool[] _slotPools;
    private readonly INodeMemoryPool[] _nodePools;
    private readonly LookupsMemoryPool _sharedLookups;

    private readonly ThreadLocal<List<RootMoveStat>> _threadLocalStatsBuffer = new(() => new List<RootMoveStat>(16));
    private readonly int[] _lastChosenIndices;
    private string[] _sortedSnakeIds = [];

    public BattleSnakeCluster(
        Engine[] engines, 
        ISlotMemoryPool[] slotPools, 
        INodeMemoryPool[] nodePools, 
        LookupsMemoryPool sharedLookups)
    {
        if (engines.Length != slotPools.Length || engines.Length != nodePools.Length) 
            throw new ArgumentException("Cluster components length mismatch.");

        _engines = engines;
        _slotPools = slotPools;
        _nodePools = nodePools;
        _sharedLookups = sharedLookups;

        _lastChosenIndices = new int[_engines.Length];
        Array.Fill(_lastChosenIndices, -1);
    }

    public void InitializeGame(string[] sortedSnakeIds)
    {
        _sortedSnakeIds = sortedSnakeIds;

        for (var i = 0; i < _engines.Length; i++)
        {
            _engines[i].InitializeGame(sortedSnakeIds);
            _lastChosenIndices[i] = -1; 
        }
    }

    public async Task<byte> ComputeMoveAsync(Request request)
    {
        // ---------------------------------------------------------
        // 1. Calcolo Hash (Via Extension Method sul Pool 0)
        // ---------------------------------------------------------
        
        var mainPool = _slotPools[0];
        
        // A. Reset preventivo
        mainPool.Reset(); 
        
        // B. Alloco uno slot temporaneo per il parsing
        var tempIndex = mainPool.Allocate();
        
        if (tempIndex == -1) return _engines[0].GetFallbackMove(); 

        // C. Calcolo Hash usando l'Extension Method
        // Questo metodo popola l'Arena e calcola lo Zobrist Hash
        var targetHash = mainPool.CalculateRequestHash(tempIndex, in request, _sortedSnakeIds);

        // D. Reset finale
        // Liberiamo lo slot temporaneo così l'Engine 0 troverà il pool pulito
        mainPool.Reset();

        // ---------------------------------------------------------
        // 2. Parallel Search
        // ---------------------------------------------------------

        var tasks = new Task[_engines.Length];
        for (var i = 0; i < _engines.Length; i++)
        {
            var index = i;
            tasks[i] = Task.Run(() => 
                _lastChosenIndices[index] = _engines[index].FindBestMove(in request, _lastChosenIndices[index], targetHash)
            );
        }

        await Task.WhenAll(tasks);

        // ---------------------------------------------------------
        // 3. Aggregazione Statistiche
        // ---------------------------------------------------------
        
        Span<long> totalVisits = stackalloc long[9]; 
        totalVisits.Clear();

        foreach (var engine in _engines)
        {
            var buffer = _threadLocalStatsBuffer.Value!;
            engine.GetRootStats(buffer);

            foreach (var stat in buffer) 
            {
                if (stat.Move < totalVisits.Length)
                {
                    totalVisits[stat.Move] += stat.Visits;
                }
            }
        }

        // 4. Selezione
        var bestMove = Moves.Up; 
        long maxVisits = -1;
        ReadOnlySpan<byte> movesToCheck = [Moves.Up, Moves.Down, Moves.Left, Moves.Right];

        foreach (var move in movesToCheck)
        {
            if (totalVisits[move] > maxVisits) 
            {
                maxVisits = totalVisits[move];
                bestMove = move;
            }
        }

        return maxVisits <= 0 ? _engines[0].GetFallbackMove() : bestMove;
    }

    public void Reset()
    {
        for (var i = 0; i < _engines.Length; i++)
        {
            _engines[i].Reset();
            _lastChosenIndices[i] = -1;
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