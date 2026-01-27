using System.Diagnostics;
using System.Runtime.CompilerServices;
using Thanos.Abstract;
using Thanos.Common;
using Thanos.SourceGen;

namespace Thanos.MCST;

public sealed class Engine
{
    private const int FIXED_POINT_FACTOR = 10000; // Deve matchare quello del Worker

    private readonly ISlotMemoryPool _slotPool;
    private readonly INodeMemoryPool _nodePool;
    private readonly IWorker[] _workers; // Array di worker per il parallelismo

    private int _rootIndex = -1; 
    private string[] _sortedSnakeIds = [];

    // Costruttore aggiornato per ricevere tutti i worker disponibili
    public Engine(ISlotMemoryPool slotPool, INodeMemoryPool nodePool, IWorker[] workers)
    {
        _slotPool = slotPool;
        _nodePool = nodePool;
        _workers = workers;
    }

    public void InitializeGame(string[] sortedSnakeIds)
    {
        _sortedSnakeIds = sortedSnakeIds;
        Reset();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Reset()
    {
        _rootIndex = -1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int FindBestMove(in Request request, int lastChosenIndex, long targetHash)
    {
        var treeReused = false;
        
        var memoryPressure = _nodePool.Index >= _nodePool.Capacity * 0.90f; // Soglia alzata al 90%

        // FASE 1: Tree Reuse
        if (!memoryPressure && _rootIndex != -1 && lastChosenIndex > 0)
        {
            var potentialRoot = FindNewRoot(lastChosenIndex, targetHash);
            if (potentialRoot > 0)
            {
                _rootIndex = potentialRoot;
                ref var newRootNode = ref _nodePool.Get(_rootIndex);
                newRootNode.NewRoot(); 

                var rootArena = _slotPool.GetArena(_rootIndex);
                rootArena.InitializeFromRequest(in request, _sortedSnakeIds);
                treeReused = true;
            }
        }

        // FASE 2: Full Reset
        if (!treeReused)
        {
            _nodePool.Reset();
            _slotPool.Reset();

            _rootIndex = _nodePool.Allocate(); // Allocazione Thread-Safe
            var slotIndex = _slotPool.Allocate();

            if (_rootIndex == -1 || slotIndex == -1) 
                throw new InvalidOperationException("Pools exhausted at start.");
            
            Debug.Assert(_rootIndex == slotIndex);

            var rootArena = _slotPool.GetArena(_rootIndex);
            rootArena.InitializeFromRequest(in request, _sortedSnakeIds);

            ref var rootNode = ref _nodePool.Get(_rootIndex);
            rootNode.PlacementRoot(targetHash); 
        }

        // FASE 3: MCTS (Parallel)
        RunIterationsParallel(request.Board.Area);

        // FASE 4: Selection
        return SelectBestChildIndex(_rootIndex);
    }

    private int FindNewRoot(int myLastMoveNodeIndex, long targetHash)
    {
        ref var myLastMoveNode = ref _nodePool.Get(myLastMoveNodeIndex);
        // Ricerca limitata in profondità per ritrovare lo stato corrente
        return FindNodeWithHash(myLastMoveNode.FirstChildIndex, targetHash, 5);
    }

    private int FindNodeWithHash(int startIndex, long targetHash, int depthLimit)
    {
        if (startIndex <= 0 || depthLimit <= 0) return 0;
        var current = startIndex;
        const int MaxSiblingsSearch = 100; // Limitiamo la ricerca orizzontale per velocità

        var count = 0;
        while (current > 0 && count++ < MaxSiblingsSearch)
        {
            ref var node = ref _nodePool.Get(current);
            if (node.Hash == targetHash) return current;

            // Ricorsione: controlliamo anche i nipoti (nel caso di mosse "environment" intermedie)
            var foundInChild = FindNodeWithHash(node.FirstChildIndex, targetHash, depthLimit - 1);
            if (foundInChild != 0) return foundInChild;
            
            current = node.NextSiblingIndex;
        }
        return 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RunIterationsParallel(int area)
    {
        const long maxTimeMs = 450;
        var stopwatch = Stopwatch.StartNew();
        
        ref var rootNode = ref _nodePool.Get(_rootIndex);

        // Quick check: se è foglia, facciamo almeno un passaggio per espanderla
        if (rootNode.IsLeafNode) 
        {
            _workers[0].RunIteration(area, _rootIndex);
        }

        // Se dopo l'espansione è già solved (es. morte immediata), usciamo
        if (rootNode.IsSolvedWin || rootNode.IsSolvedLoss) return;

        // Loop Parallelo
        // Ogni worker macina iterazioni finché non scade il tempo
        var isSolvedWin = rootNode.IsSolvedWin;
        var isSolvedLoss = rootNode.IsSolvedLoss;
        Parallel.ForEach(_workers, worker =>
        {
            // Batching locale per ridurre le chiamate a stopwatch
            const int batchSize = 100;
            while (stopwatch.ElapsedMilliseconds < maxTimeMs)
            {
                // Check condizioni di stop globali (es. Solved)
                // Nota: Leggere rootNode.IsSolved... in parallelo è safe (lettura volatile implicita)
                if (isSolvedWin || isSolvedLoss) break;

                // Esegui batch
                for (var i = 0; i < batchSize; i++)
                {
                    worker.RunIteration(area, _rootIndex);
                }
            }
        });

        stopwatch.Stop();
        
        // Log statistiche (usiamo Interlocked.Read implicitamente tramite accesso int)
        // Console.WriteLine($"[Engine] Time: {stopwatch.ElapsedMilliseconds}ms, Visits: {rootNode.Visits}");
    }

    private unsafe int SelectBestChildIndex(int rootIndex)
    {
        ref var rootNode = ref _nodePool.Get(rootIndex);
        
        var bestChildIndex = -1;
        var maxVisits = -1;
        var maxScore = int.MinValue; // Confrontiamo interi (AtomicRewards)

        var childIndex = rootNode.FirstChildIndex;
        while (childIndex != -1)
        {
            ref var child = ref _nodePool.Get(childIndex);
            
            // Logica di selezione robusta
            if (child.Visits > maxVisits)
            {
                maxVisits = child.Visits;
                maxScore = child.AtomicRewards[0]; // Hero Index = 0
                bestChildIndex = childIndex;
            }
            else if (child.Visits == maxVisits)
            {
                // Tie-break sullo score
                if (child.AtomicRewards[0] > maxScore)
                {
                    maxScore = child.AtomicRewards[0];
                    bestChildIndex = childIndex;
                }
            }
            childIndex = child.NextSiblingIndex;
        }
        
        return bestChildIndex;
    }
    
    public unsafe void GetRootStats(List<RootMoveStat> outputBuffer)
    {
        outputBuffer.Clear();
        if (_rootIndex <= 0) return;

        ref var rootNode = ref _nodePool.Get(_rootIndex);
        var childIndex = rootNode.FirstChildIndex;

        while (childIndex != -1)
        {
            ref var childNode = ref _nodePool.Get(childIndex);
            if (childNode.Visits > 0)
            {
                // Convertiamo Fixed-Point -> Float per la visualizzazione/debug
                var avgScore = (double)childNode.AtomicRewards[0] / childNode.Visits / FIXED_POINT_FACTOR;
                outputBuffer.Add(new RootMoveStat(childNode.Move, childNode.Visits, (float)avgScore));
            }
            childIndex = childNode.NextSiblingIndex;
        }
    }

    public byte GetFallbackMove()
    {
        // if (_rootIndex <= 0) return Moves.Up;
        // var arena = _slotPool.GetArena(_rootIndex);
        // var me = arena.System[0];
        //
        // // Usiamo la logica dell'Arena per trovare una mossa legale qualsiasi
        // var legalMoves = arena.GetLegalMoves(me.Head, me.Tail, me.PreTail, 0);
        //
        // if ((legalMoves & Moves.Up) != 0) return Moves.Up;
        // if ((legalMoves & Moves.Down) != 0) return Moves.Down;
        // if ((legalMoves & Moves.Left) != 0) return Moves.Left;
        // if ((legalMoves & Moves.Right) != 0) return Moves.Right;
        
        return Moves.Up;
    }
}