using System.Diagnostics;
using System.Runtime.CompilerServices;
using Thanos.Abstract;
using Thanos.Common;
using Thanos.Memory;
using Thanos.SourceGen;

namespace Thanos.MCST;

public class Engine
{
    private readonly INodeMemoryPool _nodePool;
    private readonly ISlotMemoryPool _slotPool;
    private readonly Worker _worker;

    private int _rootIndex = NodeMemoryPool.FirstIndex; // Usa la costante definita nel pool
    
    // Stato del match corrente
    private string[] _sortedSnakeIds = [];

    public Engine(ISlotMemoryPool slotPool, INodeMemoryPool nodePool)
    {
        _slotPool = slotPool;
        _nodePool = nodePool;
        
        // Assumo che anche Worker sia stato aggiornato per accettare le interfacce
        _worker = new Worker(_slotPool, _nodePool);
    }

    /// <summary>
    /// Configura l'engine per la partita corrente.
    /// </summary>
    public void InitializeGame(string[] sortedSnakeIds, int count)
    {
        _sortedSnakeIds = sortedSnakeIds;
        // Configura il pool (imposta active snakes per SnakesSystem)
        _slotPool.Configure(count);
        // Resetta il worker se necessario
        _worker.Reset(count);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int FindBestMove(in Request request, int lastChosenIndex, long targetHash)
    {
        // 1. Tree Reuse Logic
        if (lastChosenIndex > 0)
        {
            _rootIndex = FindNewRoot(lastChosenIndex, targetHash);

            if (_rootIndex > 0)
            {
                ref var newRootNode = ref _nodePool.Get(_rootIndex);
                newRootNode.NewRoot();

                // Re-inizializziamo lo stato dell'Arena root per assicurarci che sia sincronizzato col turno corrente
                var rootArena = _slotPool.GetArena(_rootIndex);
                rootArena.InitializeFromRequest(in request, _sortedSnakeIds);
            }
        }
        else
        {
            _rootIndex = 0;
        }

        // 2. Full Reset Fallback
        if (_rootIndex <= 0)
        {
            _rootIndex = NodeMemoryPool.FirstIndex;
            
            // Prepariamo la Root Arena
            var rootIndex = _slotPool.Allocate(); // Alloca slot per la root
            
            // Safety check: se allocation fallisce (pool pieno), crashiamo o gestiamo
            if (rootIndex == -1) throw new InvalidOperationException("SlotPool exhausted at root allocation.");

            var rootArena = _slotPool.GetArena(rootIndex); // Nota: Engine usava _rootIndex come indice slot, assumiamo mappatura 1:1 o logica interna
            
            // Qui passiamo gli ID ordinati
            rootArena.InitializeFromRequest(in request, _sortedSnakeIds);

            ref var rootNode = ref _nodePool.Get(_rootIndex);
            rootNode.PlacementRoot(targetHash);
            
            // Aggiorniamo il riferimento allo slot nel nodo se necessario, 
            // oppure assumiamo che il Worker sappia che Node X usa Slot X.
        }

        RunIterations(request.Board.Area);

        // 3. Selection
        return _nodePool.SelectMostVisitedChild(_rootIndex);
    }

    private int FindNewRoot(int myLastMoveNodeIndex, long targetHash)
    {
        ref var myLastMoveNode = ref _nodePool.Get(myLastMoveNodeIndex);
        return FindNodeWithHash(myLastMoveNode.FirstChildIndex, targetHash, 5);
    }

    private int FindNodeWithHash(int startIndex, long targetHash, int depthLimit)
    {
        if (startIndex <= 0 || depthLimit <= 0) return 0;

        var current = startIndex;
        var safetyCounter = 0;
        const int MaxSiblingsSearch = 10000;

        while (current > 0 && safetyCounter++ < MaxSiblingsSearch)
        {
            ref var node = ref _nodePool.Get(current);

            if (node.Hash == targetHash) return current;

            var foundInChild = FindNodeWithHash(node.FirstChildIndex, targetHash, depthLimit - 1);
            if (foundInChild != 0) return foundInChild;

            current = node.NextSiblingIndex;
        }

        return 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RunIterations(int area)
    {
        const long maxTimeMs = 450;
        const long forcedMoveTimeMs = 50;

        var stopwatch = Stopwatch.StartNew();

        ref var rootNode = ref _nodePool.Get(_rootIndex);

        // Se la root è foglia (appena creata), espandiamola subito
        if (rootNode.IsLeafNode) 
        {
             _worker.RunIteration(area, _rootIndex);
        }

        // Contiamo i figli per decidere il time management
        var childCount = 0;
        var childIdx = rootNode.FirstChildIndex;
        while (childIdx != -1)
        {
            childCount++;
            childIdx = _nodePool.Get(childIdx).NextSiblingIndex;
        }

        var timeLimit = childCount <= 1 ? forcedMoveTimeMs : maxTimeMs;
        var counter = 0;

        while (stopwatch.ElapsedMilliseconds < timeLimit)
        {
            if (rootNode.IsSolvedWin || rootNode.IsSolvedLoss) break;

            var remainingTime = timeLimit - stopwatch.ElapsedMilliseconds;

            // Batching dinamico per ridurre overhead del controllo tempo
            var currentBatchSize = remainingTime switch
            {
                > 250 => 2048,
                > 150 => 1024,
                > 80 => 512,
                _ => 256
            };

            for (var i = 0; i < currentBatchSize; i++) 
            {
                _worker.RunIteration(area, _rootIndex);
            }
            counter += currentBatchSize;
        }

        stopwatch.Stop();
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

            if (childNode.Visits > 0 || childNode.IsSolvedWin)
            {
                // Score calcolato sui float
                var avgScore = childNode.Visits > 0 ? childNode.Score / childNode.Visits : -1;
                outputBuffer.Add(new RootMoveStat(childNode.Move, childNode.Visits, avgScore));
            }

            childIndex = childNode.NextSiblingIndex;
        }
    }

    public byte GetFallbackMove()
    {
        if (_rootIndex <= 0) return Moves.Up;

        var arena = _slotPool.GetArena(_rootIndex);
        var me = arena.System[0];

        // 0 è sempre il nostro indice (Hero) grazie al mapping dell'Agente
        var legalMoves = arena.GetLegalMoves(me.Head, me.Tail, me.ElementBeforeTail, 0);

        if ((legalMoves & Moves.Up) != 0) return Moves.Up;
        if ((legalMoves & Moves.Down) != 0) return Moves.Down;
        if ((legalMoves & Moves.Left) != 0) return Moves.Left;
        if ((legalMoves & Moves.Right) != 0) return Moves.Right;

        return Moves.Up;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Reset()
    {
        _rootIndex = 0;
        // Non resettiamo _worker.Reset(count) qui perché il count potrebbe cambiare
        // Lo facciamo in InitializeGame
    }
}