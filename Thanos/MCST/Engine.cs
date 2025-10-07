using System.Diagnostics;
using Thanos.Common;
using Thanos.Memory;
using Thanos.SourceGen;
using System.Text; // Aggiunto per StringBuilder

namespace Thanos.MCST;

public class Engine
{
    private readonly NodeMemoryPool _nodePool;
    private readonly SlotMemoryPool _slotPool;
    private readonly Worker _worker;

    private int _rootIndex;
    private long _rootHash;

    public Engine(SlotMemoryPool slotPool, NodeMemoryPool nodePool)
    {
        _slotPool = slotPool;
        _nodePool = nodePool;
        _worker = new Worker(_slotPool, _nodePool); // Passa la mappa al Worker
    }

    public int FindBestMove(in Request request)
    {
        // Se _rootIndex è 0, significa che siamo al primo turno o c'è stato un reset. Dobbiamo creare la radice da zero.
        if (_rootIndex == 0)
        {
            Console.WriteLine("[Engine] Creating new MCTS tree from scratch.");
            _worker.Reset(1, request.Game.Ruleset.Settings);

            var rootArena = _slotPool.GetArena(1);
            rootArena.InitializeFromRequest(in request);

            _rootHash = ZobristHasher.CalculateHash(rootArena);

            ref var rootNode = ref _nodePool[1]; // Usa l'indice 1 come radice
            rootNode.PlacementRoot(-1, Moves.None, _rootHash);

            _rootIndex = 1;
        }
        else
        {
            // Se siamo qui, PrepareNextTurn ha funzionato!
            // La radice è già impostata. Dobbiamo solo aggiornare il suo stato
            // con i dati reali della richiesta, perché quello attuale è simulato.
            Console.WriteLine($"[Engine] Updating root node {_rootIndex} with new board state.");
            var rootSlot = _slotPool.GetArena(_rootIndex);
            rootSlot.InitializeFromRequest(in request);
        }

        var stopwatch = Stopwatch.StartNew();
        var counter = 0;
        while (stopwatch.ElapsedMilliseconds < 450) // Limite di tempo per l'iterazione
        {
            _worker.RunIteration(_rootIndex);
            counter++;
        }
        stopwatch.Stop();

        Console.WriteLine($"[MCE] Iterations completed: {counter} in {stopwatch.ElapsedMilliseconds}ms.");

        ref var finalRootNode = ref _nodePool[_rootIndex];
        var bestChildIndex = finalRootNode.SelectMostVisitedChild(_nodePool);

        // LOGGING: Mostra le statistiche dei figli della radice prima di decidere
        if (bestChildIndex != -1)
        {
            var logBuilder = new StringBuilder();
            logBuilder.AppendLine($"[Engine] Decision Analysis for Root Node {_rootIndex} (Total Visits: {finalRootNode.Visits}):");
            var childIndex = finalRootNode.FirstChildIndex;
            while(childIndex != -1)
            {
                ref var childNode = ref _nodePool[childIndex];
                logBuilder.AppendLine($"  -> Move: {ToApiMove(childNode.Move),-5} | Visits: {childNode.Visits,-7} | Win Rate: {(childNode.Wins / childNode.Visits):P2}");
                childIndex = childNode.NextSiblingIndex;
            }
            ref var bestNode = ref _nodePool[bestChildIndex];
            logBuilder.AppendLine($"[Engine] Best Move Selected: {ToApiMove(bestNode.Move)} with {bestNode.Visits} visits.");
            Console.WriteLine(logBuilder.ToString());
        }
        else
        {
            Console.WriteLine("[Engine] CRITICAL: No valid child found from root node.");
        }


        return bestChildIndex;
    }

    public void Reset()
    {
        Console.WriteLine("[Engine] Resetting MCTS tree.");
        _rootIndex = 0;
        _worker.Reset(1); // Resetta il worker per iniziare ad allocare dal prossimo ID disponibile.
    }

    public bool PrepareNextTurn(int lastChosenIndex, long currentBoardHash)
    {
        if (lastChosenIndex == 0)
        {
             Console.WriteLine("[Engine] Cannot reuse tree, no previous move exists.");
             return false;
        }

        var childIndex = _nodePool[_rootIndex].FirstChildIndex;
        while (childIndex != -1)
        {
            ref var childNode = ref _nodePool[childIndex];
            if (childNode.Hash == currentBoardHash)
            {
                // Trovato! Promuoviamo questo nodo a nuova radice.
                Console.WriteLine($"[Engine] Cache HIT! Reusing subtree from node {childIndex}. New root.");
                _rootIndex = childIndex;
                ref var newRoot = ref _nodePool[_rootIndex];
                newRoot.ParentIndex = -1;
                newRoot.Generation = 0;
                
                var maxId = _worker.GetMaxId(_rootIndex);
                _worker.Reset(maxId + 1);
                 Console.WriteLine($"[Engine] Worker reset to start allocating from ID {maxId + 1}.");

                _rootHash = currentBoardHash;
                return true;
            }

            childIndex = childNode.NextSiblingIndex;
        }

        Console.WriteLine("[Engine] Cache MISS! No matching child node found. Resetting tree.");
        Reset();
        return false;
    }
        
    // Metodo helper per convertire la mossa in stringa per i log
    private static string ToApiMove(byte move) =>
        move switch
        {
            Moves.Up => "up",
            Moves.Down => "down",
            Moves.Left => "left",
            Moves.Right => "right",
            _ => "none"
        };
}