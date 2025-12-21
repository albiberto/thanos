using Thanos.Abstract;
using Thanos.Common;
using Thanos.Extensions;
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
    
    private readonly ISlotMemoryPool _hashCalculationPool; 

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

        // --- FIX: Inizializza il pool per l'hash ---
        // Basta una capacità piccolissima (es. 5 slot), serve solo per 1 istante.
        var slotLayout = new SlotMemoryLayout(Constants.Medium.Area, 64, Constants.MaxSnakesCount);
        _hashCalculationPool = new SlotMemoryPool(5, 0, Constants.MaxSnakesCount, sharedLookups, slotLayout);
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
        // 1. Calcolo Hash (Su Pool ISOLATO)
        // ---------------------------------------------------------
        
        // Reset del pool di calcolo (non tocca gli engine!)
        _hashCalculationPool.Reset(); 
        
        var tempIndex = _hashCalculationPool.Allocate();
        
        // Se per assurdo fallisce l'allocazione su 5 slot (impossibile), fallback
        if (tempIndex == -1) return _engines[0].GetFallbackMove(); 

        // Calcolo Hash popolando l'Arena temporanea
        var targetHash = _hashCalculationPool.CalculateRequestHash(tempIndex, in request, _sortedSnakeIds);

        // Ora _hashCalculationPool è sporco, ma verrà resettato al prossimo turno.
        // I pool _slotPools[0..N] sono INTATTI e pronti per il Tree Reuse.

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
        
        _hashCalculationPool.Dispose(); // Ricordati di disporre anche questo!
        
        _sharedLookups.Dispose();
        _threadLocalStatsBuffer.Dispose();
    }
}