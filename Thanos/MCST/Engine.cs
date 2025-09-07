using System.Diagnostics;
using Thanos.Common;
using Thanos.Memory;
using Thanos.PreWarm.Memory;
using Thanos.SourceGen;

namespace Thanos.MCST;

public class Engine
{
    private readonly NodeMemoryPool _nodePool;
    private readonly SlotMemoryPool _slotPool;

    private int _rootIndex;
    private Worker _worker;

    public Engine(SlotMemoryPool slotPool, NodeMemoryPool nodePool)
    {
        _slotPool = slotPool;
        _nodePool = nodePool;
        _worker = new Worker(_slotPool, _nodePool, new Luts());
    }

    public int FindBestMove(in Request request)
    {
        // Se _rootIndex è 0, significa che siamo al primo turno o c'è stato un reset.
        // Dobbiamo creare la radice da zero.
        if (_rootIndex == 0)
        {
            // L'ID 0 è riservato per la radice.
            _worker.Reset(1); // Iniziamo ad allocare dal prossimo ID disponibile.

            var rootArena = _slotPool[_rootIndex]; // Usa l'indice 0
            rootArena.InitializeFromRequest(in request);

            var hash = ZobristHasher.CalculateHash(rootArena);

            ref var rootNode = ref _nodePool[_rootIndex]; // Usa l'indice 0
            rootNode.PlacementNew(-1, Moves.None, hash);
        }
        else
        {
            // Se siamo qui, PrepareNextTurn ha funzionato!
            // La radice è già impostata. Dobbiamo solo aggiornare il suo stato
            // con i dati reali della richiesta, perché quello attuale è simulato.
            var rootSlot = _slotPool[_rootIndex];
            rootSlot.InitializeFromRequest(in request);
            // Non ricalcoliamo l'hash qui, ci fidiamo di quello verificato in PrepareNextTurn
        }

        var stopwatch = Stopwatch.StartNew();
        var counter = 0;
        // while (stopwatch.ElapsedMilliseconds < 450) // Limite di tempo per l'iterazione
        while (counter < 500)
        {
            _worker.RunIteration(_rootIndex);
            counter++;
        }

        Console.WriteLine($"[MCE] Iterazioni completate: {counter}");

        ref var finalRootNode = ref _nodePool[_rootIndex];

        var bestChildIndex = finalRootNode.SelectMostVisitedChild(_nodePool);
        return bestChildIndex;
    }

    public void Reset(in Luts? luts = null)
    {
        if (luts.HasValue) _worker = new Worker(_slotPool, _nodePool, luts.Value);

        _rootIndex = 0;
        _worker.Reset(1); // Resetta il worker per iniziare ad allocare dal prossimo ID disponibile.
    }

    public void PrepareNextTurn(int previousChosenNodeIndex, in Request newTurnRequest, Dictionary<string, int> snakeIdMap)
    {
        if (previousChosenNodeIndex == 0)
        {
            Reset();
            return;
        }

        var realStateHash = ZobristHasher.CalculateHash(in newTurnRequest, snakeIdMap);
        ref var chosenNode = ref _nodePool[previousChosenNodeIndex];

        if (chosenNode.Hash != realStateHash)
        {
            // Console.WriteLine("[MCE] Cache MISS! Hash non corrispondenti. Reset dell'albero.");
            Reset();
            return;
        }

        // Console.WriteLine($"[MCE] Cache HIT! Riutilizzo dell'albero dalla radice {previousChosenNodeIndex}.");
        _rootIndex = previousChosenNodeIndex;
        ref var newRoot = ref _nodePool[_rootIndex];
        newRoot.ParentIndex = -1;

        // Dobbiamo dire al worker da quale ID ripartire per le nuove allocazioni!
        // Questo richiede di trovare l'ID più alto nell'albero, un'operazione che possiamo aggiungere.
        // Per ora, lo lasciamo continuare a contare, ma questo andrà corretto.
    }
}