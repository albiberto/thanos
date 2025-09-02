using System.Diagnostics;
using Thanos.Common;
using Thanos.Memory;
using Thanos.SourceGen;

namespace Thanos.MCST;

public class MonteCarloEngine
{
    private readonly NodeMemoryPool _nodePool;
    private readonly SlotMemoryPool _slotPool;
    private readonly Worker _worker;

    public int _rootIndex;

    public MonteCarloEngine(SlotMemoryPool slotPool, NodeMemoryPool nodePool)
    {
        _slotPool = slotPool;
        _nodePool = nodePool;
        _worker = new Worker(_slotPool, _nodePool);
    }

    public int FindBestMove(in Request request)
    {
        if (_rootIndex == 0)
        {
            var rootSlot = _slotPool[_rootIndex];
            rootSlot.InitializeFromRequest(in request);

            var hash = ZobristHasher.CalculateHash(rootSlot.Arena);

            ref var rootNode = ref _nodePool[_rootIndex];
            rootNode.Initialize(-1, Moves.None, _rootIndex, hash);
        }

        var stopwatch = Stopwatch.StartNew();
        var counter = 0;
        // while (stopwatch.ElapsedMilliseconds < 450)
        while (counter < 10)
        {
            _worker.RunIteration(_rootIndex);
            counter++;
        }

        Console.WriteLine($"[MCST] Iterations: {counter} in {stopwatch.ElapsedMilliseconds}ms");
        Console.WriteLine();
        Console.WriteLine("[MCST] ==========================================================");
        Console.WriteLine("[MCST] ==========================================================");
        Console.WriteLine("[MCST] ==========================================================");
        Console.WriteLine();

        ref var finalRootNode = ref _nodePool[_rootIndex];

        var bestChildIndex = finalRootNode.SelectMostVisitedChild(_nodePool);
        return bestChildIndex;
    }

    public void Reset() => _rootIndex = 0;

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
        _rootIndex = previousChosenNodeIndex;
        ref var newRoot = ref _nodePool[_rootIndex];
        newRoot.ParentIndex = -1; // Taglia il collegamento con il suo vecchio genitore.
    }
}