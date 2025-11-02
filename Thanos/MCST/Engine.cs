using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Thanos.Memory;
using Thanos.SourceGen;
using Thanos.Extensions;

namespace Thanos.MCST;

public class Engine
{
    private readonly NodeMemoryPool _nodePool;
    private readonly SlotMemoryPool _slotPool;
    private readonly Worker _worker;

    private int _rootIndex;

    public Engine(SlotMemoryPool slotPool, NodeMemoryPool nodePool)
    {
        _slotPool = slotPool;
        _nodePool = nodePool;
        _worker = new Worker(_slotPool, _nodePool);
        _rootIndex = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int FindBestMove(in Request request, int previousMoveIndex)
    {
        if (previousMoveIndex > 0)
        {
            _rootIndex = FindNewRoot(previousMoveIndex, in request);

            if (_rootIndex > 0)
            {
                ref var newRootNode = ref _nodePool[_rootIndex];
                newRootNode.NewRoot();

                var rootArena = _slotPool.GetArena(_rootIndex);
                rootArena.InitializeFromRequest(in request);
            }
        }
        else
        {
            _rootIndex = 0;
        }

        if (_rootIndex == 0)
        {
            _rootIndex = 1;
            _worker.Reset(_rootIndex, request.Game.Ruleset.Settings);

            var hash = _slotPool.CalculateHash(_rootIndex, in request);
            ref var rootNode = ref _nodePool[_rootIndex];
            rootNode.PlacementRoot(hash);

            var rootArena = _slotPool.GetArena(_rootIndex);
            rootArena.InitializeFromRequest(in request);
        }

        RunIterations(request.Board.Area);

#if DEBUG
        LogFullTreeState();
#endif

        return _nodePool.SelectMostVisitedChild(_rootIndex);
    }

    private int FindNewRoot(int myLastMoveNodeIndex, in Request request)
    {
        var currentHash = _slotPool.CalculateHash(1, in request);

        ref var myLastMoveNode = ref _nodePool[myLastMoveNodeIndex];
        if (myLastMoveNode.IsLeafNode) return 0;

        var childIndex = myLastMoveNode.FirstChildIndex;
        while (childIndex != -1)
        {
            ref var childNode = ref _nodePool[childIndex];

#if DEBUG
            Console.WriteLine($"[Engine] Comparing Child Node Hash: {childNode.Hash} with Current Hash: {currentHash}");
#endif

            if (childNode.Hash == currentHash) return childIndex;

            childIndex = childNode.NextSiblingIndex;
        }

        return 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RunIterations(int area, int counter = 0)
    {
        var stopwatch = Stopwatch.StartNew();

        // while (stopwatch.ElapsedMilliseconds < 450)
        while (counter < 10)
        {
            _worker.RunIteration(area, _rootIndex);
            counter++;
        }

        stopwatch.Stop();

        Console.WriteLine($"[Engine] Completed {counter} iterations in {stopwatch.ElapsedMilliseconds} ms.");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Reset()
    {
        _rootIndex = 0;
        _worker.Reset(1);
    }

    /// <summary>
    /// Esegue il dump di tutti i nodi nell'albero di ricerca ATTIVO
    /// partendo dal nodo root attuale.
    /// </summary>
    public void LogFullTreeState()
    {
#if DEBUG
        Console.WriteLine($"[Engine] --- INIZIO LOG COMPLETO ALBERO (Partendo da root: {_rootIndex}) ---");

        // Avvia la visita ricorsiva dall'indice della root attuale
        LogNodeRecursive(_rootIndex);

        Console.WriteLine($"[Engine] --- FINE LOG COMPLETO ALBERO ---");
#endif
    }

    /// <summary>
    /// Metodo helper ricorsivo per visitare e loggare un nodo e tutti i suoi discendenti.
    /// </summary>
    private void LogNodeRecursive(int nodeIndex)
    {
#if DEBUG
        // Caso base: se l'indice non è valido (es. fine lista fratelli), fermati.
        if (nodeIndex == -1) return;

        // 1. Logga il nodo corrente
        Console.WriteLine($"--- [Stato Nodo {nodeIndex}] ---");
        Log(nodeIndex); // Chiama il tuo metodo di log esistente
        Console.WriteLine($"-------------------------");

        // 2. Ottieni il riferimento al nodo per trovare i suoi figli
        ref var node = ref _nodePool[nodeIndex];

        // 3. Itera su tutti i figli (usando la lista linkata FirstChild/NextSibling)
        var childIndex = node.FirstChildIndex;
        while (childIndex != -1)
        {
            // 4. Chiamata ricorsiva per ogni figlio
            LogNodeRecursive(childIndex);

            // 5. Passa al prossimo fratello
            ref var childNode = ref _nodePool[childIndex];
            childIndex = childNode.NextSiblingIndex;
        }
#endif
    }


    private void Log(int childIndex)
    {
#if DEBUG
        // --- CORREZIONE BUG ---
        // La variabile deve essere 'childNodeRef' come l'hai dichiarata,
        // o rinominiamo la variabile in 'childNode' per coerenza.
        ref var childNode = ref _nodePool[childIndex]; // Rinominata per coerenza

        Console.WriteLine($"[Engine] Checking Child Index: {childIndex}");
        Console.WriteLine($"[Engine] Node: {JsonSerializer.Serialize(childNode)}"); // Ora 'childNode' è corretta
        var arena = _slotPool.GetArena(childIndex);

        Console.WriteLine($"[Engine] Arena State for Child Index {childIndex}:");
        Console.WriteLine($"{arena.Snakes.ToGridString(11, 11)}");

        var system = arena.System;
        var totalSnakes = system.Count;

        for (var i = 0; i < totalSnakes; i++)
        {
            var snake = system[i];
            Console.WriteLine($"[Engine] Snake {i}");
            Console.WriteLine($"    Head: {snake.Head}");
            Console.WriteLine($"    Tail: {snake.Tail}");
            Console.WriteLine($"    Length: {snake.Length}");
            Console.WriteLine($"    Health: {snake.HP}");
            Console.WriteLine($"    IsDead: {snake.IsDead}");
            Console.WriteLine($"    Body Bitboard: {snake.Body.ToGridString(11, 11)}");
            Console.WriteLine($"    CircularBuffer: {string.Join(" -> ", snake._queue.Buffer.ToArray())}");
            Console.WriteLine($"    Head: {snake._queue.PeekHead}");
            Console.WriteLine($"    Tail: {snake._queue.PeekTail}");
            Console.WriteLine($"    HeadIndex: {snake._queue._state.HeadIndex}");
            Console.WriteLine($"    TailIndex: {snake._queue._state.TailIndex}");
        }
#endif
    }
}