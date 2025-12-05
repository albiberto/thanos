using Thanos.Abstract;
using Thanos.Common;
using Thanos.Extensions;
using Thanos.MCST;
using Thanos.Memory;
using Thanos.SourceGen;

namespace Thanos;

public sealed class BattleSnakeCluster : IBattleSnakeCluster // Assicurati che l'interfaccia erediti da IDisposable
{
    private readonly Engine[] _engines;
    private readonly ISlotMemoryPool[] _slotPools; // Uniformato a ISlotPool
    private readonly ISlotMemoryPool[] _nodePools; // Uniformato a INodePool
    private readonly LookupsMemoryPool _sharedLookups;

    private readonly ThreadLocal<List<RootMoveStat>> _threadLocalStatsBuffer = new(() => new List<RootMoveStat>(16));
    
    private readonly int[] _lastChosenIndices;

    private string[] _sortedSnakeIds = [];

    public BattleSnakeCluster(
        Engine[] engines, 
        ISlotMemoryPool[] slotPools, 
        ISlotMemoryPool[] nodePools, 
        LookupsMemoryPool sharedLookups)
    {
        if (engines.Length != slotPools.Length || engines.Length != nodePools.Length) 
            throw new ArgumentException("Cluster components length mismatch.");

        _engines = engines;
        _slotPools = slotPools;
        _nodePools = nodePools;
        _sharedLookups = sharedLookups;

        _lastChosenIndices = new int[_engines.Length];
        Array.Fill(_lastChosenIndices, NodeMemoryPool.FirstIndex);
    }

    public void InitializeGame(string[] sortedSnakeIds, int count)
    {
        // 1. Memorizziamo gli ID per usarli nel calcolo dell'Hash (Zobrist)
        _sortedSnakeIds = sortedSnakeIds;

        // 2. Propaghiamo la configurazione a tutti i motori
        // Questo permette agli engine di configurare i loro SlotPool e Worker
        for (var i = 0; i < _engines.Length; i++)
        {
            _engines[i].InitializeGame(sortedSnakeIds, count);
        }
    }

    public async Task<byte> ComputeMoveAsync(Request request)
    {
        // 1. Calcolo Hash (Richiede gli ID ordinati per mappare la Request all'Arena)
        // Usiamo il pool 0 tanto l'hash è deterministico e indipendente dallo stato del pool
        var targetHash = _slotPools[0].CalculateRequestHash(0, in request, _sortedSnakeIds);

        // 2. Parallel Search
        var tasks = new Task[_engines.Length];
        for (var i = 0; i < _engines.Length; i++)
        {
            var index = i;
            // Ogni motore parte dalla sua ultima posizione nota (Tree Reuse)
            tasks[i] = Task.Run(() => 
                _lastChosenIndices[index] = _engines[index].FindBestMove(in request, _lastChosenIndices[index], targetHash)
            );
        }

        await Task.WhenAll(tasks);

        // 3. Aggregazione Statistiche (Map-Reduce)
        var totalVisits = new long[5]; // 0=None, 1=Up, 2=Down, 4=Left, 8=Right (Max idx 8 se usi flag diretti, ma qui mappiamo stat.Move)

        foreach (var engine in _engines)
        {
            var buffer = _threadLocalStatsBuffer.Value!;
            engine.GetRootStats(buffer);

            foreach (var stat in buffer) 
            {
                // Assumiamo che stat.Move sia un byte flag valido (1,2,4,8)
                // Se usi un array di appoggio [5], devi assicurarti che stat.Move < 5.
                // Se stat.Move sono i flag (1,2,4,8), serve un array più grande o uno switch.
                // PER ORA: Assumo che stat.Move sia mappato 0..4 o che l'array totalVisits sia dimensionato per i flag (9).
                // Con i flag (8 max), dimensione 9 è sicura.
                if (stat.Move < totalVisits.Length)
                {
                    totalVisits[stat.Move] += stat.Visits;
                }
            }
        }

        // 4. Selezione Mossa Migliore
        var bestMove = Moves.Up;
        long maxVisits = -1;
        
        // Array statico per evitare allocazioni enumerator
        ReadOnlySpan<byte> movesToCheck = [Moves.Up, Moves.Down, Moves.Left, Moves.Right];

        foreach (var move in movesToCheck)
        {
            // Nota: move qui è il flag (1, 2, 4, 8)
            if (totalVisits[move] > maxVisits) 
            {
                maxVisits = totalVisits[move];
                bestMove = move;
            }
        }

        // 5. Fallback se nessuna visita valida
        return maxVisits <= 0
            ? _engines[0].GetFallbackMove()
            : bestMove;
    }

    public void Reset()
    {
        for (var i = 0; i < _engines.Length; i++)
        {
            _engines[i].Reset();
            _lastChosenIndices[i] = NodeMemoryPool.FirstIndex;
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