using System.Diagnostics;
using System.Text.Json;
using Thanos.Common;
using Thanos.Memory;
using Thanos.SourceGen;

namespace Thanos.MCST;

public class MonteCarloEngine
{
    private readonly WarMemoryPool _warPool;
    private readonly NodeMemoryPool _nodePool;
    private readonly Worker _worker;
    
    public int _currentRootIndex; 

    public MonteCarloEngine(WarMemoryPool warPool, NodeMemoryPool nodePool)
    {
        _warPool = warPool;
        _nodePool = nodePool;
        _worker = new Worker(_warPool, _nodePool);
    }
    
    public int FindBestMove(in Request request)
    {
        var rootSlot = _warPool.GetNext();
        rootSlot.InitializeFromRequest(in request);

        if (_currentRootIndex == 0)
        {
            _currentRootIndex = _nodePool.GetNextIndex();
            ref var rootIndex = ref _nodePool[_currentRootIndex];
            rootIndex.Initialize(-1, Moves.None);
        }

        var stopwatch = Stopwatch.StartNew();
        var counter = 0;
        // while (stopwatch.ElapsedMilliseconds < 450)
        while (counter < 10)
        {
            _worker.RunIteration(_currentRootIndex, in rootSlot);
            counter++;
        }
        
        Console.WriteLine($"[MCST] Iterations: {counter} in {stopwatch.ElapsedMilliseconds}ms");
        Console.WriteLine();
        Console.WriteLine("[MCST] ==========================================================");
        Console.WriteLine("[MCST] ==========================================================");
        Console.WriteLine("[MCST] ==========================================================");
        Console.WriteLine();
        
        ref var finalRootNode = ref _nodePool[_currentRootIndex];
        
        var bestChildIndex = finalRootNode.SelectMostVisitedChild(_nodePool);
        return bestChildIndex;
    }

    public void Reset() => _currentRootIndex = 0;
    
    public void PrepareNextTurn(int previousChosenNodeIndex, in Request newTurnRequest, Dictionary<string, int> snakeIdMap)
    {
        if (previousChosenNodeIndex == 0)
        {
            Reset();
            return;
        }

        // 1. Calcola l'hash dello stato REALE in cui ci troviamo ora.
        var realStateHash = ZobristHasher.CalculateHash(in newTurnRequest, snakeIdMap);

        // 2. Il nodo scelto al turno precedente (`previousChosenNodeIndex`) è la nostra nuova radice.
        ref var chosenNode = ref _nodePool[previousChosenNodeIndex];

        // 3. Verifica di coerenza (opzionale ma consigliata)
        // Se l'hash non corrisponde, qualcosa è andato storto nella simulazione vs realtà.
        // In questo caso, è più sicuro resettare tutto.
        if (chosenNode.StateHash != realStateHash)
        {
            Reset(); 
            return;
        }

        // 4. Cache Hit! Promuovi il nodo scelto a nuova radice.
        _currentRootIndex = previousChosenNodeIndex;
        ref var newRoot = ref _nodePool[_currentRootIndex];
        newRoot.ParentIndex = -1; // Taglia il collegamento con il suo vecchio genitore.
    }
}