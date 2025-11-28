using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
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

    private int _rootIndex = Constants.FirstRootNodeIndex;

    public Engine(SlotMemoryPool slotPool, NodeMemoryPool nodePool)
    {
        _slotPool = slotPool;
        _nodePool = nodePool;
        _worker = new Worker(_slotPool, _nodePool);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int FindBestMove(in Request request, int lastChosenIndex, long targetHash)
    {
        // 1. Tree Reuse Logic
        // Tentiamo di trovare il nodo corrispondente al nuovo stato nell'albero esistente
        if (lastChosenIndex > 0)
        {
            _rootIndex = FindNewRoot(lastChosenIndex, targetHash);
            
            if (_rootIndex > 0)
            {
                // Trovato! Promuoviamo questo nodo a nuova radice
                ref var newRootNode = ref _nodePool[_rootIndex];
                newRootNode.NewRoot();
                
                // Aggiorniamo l'Arena con i dati freschi della Request 
                // (per correggere eventuali discrepanze di simulazione)
                var rootArena = _slotPool.GetArena(_rootIndex);
                rootArena.InitializeFromRequest(in request);
            }
        }
        else
        {
            _rootIndex = 0; // Forza il reset se non avevamo una mossa precedente valida
        }

        // 2. Full Reset Fallback
        // Se il riutilizzo è fallito o non era possibile, resettiamo tutto
        if (_rootIndex <= 0)
        {
            _rootIndex = Constants.FirstRootNodeIndex; // Di solito 1
            _worker.Reset(_rootIndex, request.Game.Ruleset.Settings);

            // Inizializziamo l'Arena della radice
            var rootArena = _slotPool.GetArena(_rootIndex);
            rootArena.InitializeFromRequest(in request);
            
            // Inizializziamo il Nodo radice usando l'hash calcolato esternamente
            ref var rootNode = ref _nodePool[_rootIndex];
            rootNode.PlacementRoot(targetHash);
        }

        RunIterations(request.Board.Area);

        // 3. Selection
        return _nodePool.SelectMostVisitedChild(_rootIndex);
    }

    private int FindNewRoot(int myLastMoveNodeIndex, long targetHash)
    {
        ref var myLastMoveNode = ref _nodePool[myLastMoveNodeIndex];

        // Cerchiamo tra i discendenti (gestendo la profondità dinamica dovuta a EnemyMoves/Environment)
        // Passiamo 5 come profondità massima di ricerca
        return FindNodeWithHash(myLastMoveNode.FirstChildIndex, targetHash, 5);
    }
    
    private int FindNodeWithHash(int startIndex, long targetHash, int depthLimit)
    {
        // FIX LOOP INFINITO: Controlliamo <= 0. 
        // 0 è la memoria di default (bug o nodo vuoto), -1 è il terminatore.
        if (startIndex <= 0 || depthLimit <= 0) return 0;

        var current = startIndex;
        
        // FIX LOOP INFINITO: Safety Counter per evitare blocchi su grafi ciclici corrotti
        var safetyCounter = 0;
        const int MaxSiblingsSearch = 10000; 

        while (current > 0 && safetyCounter++ < MaxSiblingsSearch)
        {
            ref var node = ref _nodePool[current];
            
            if (node.Hash == targetHash) return current;

            // Deep search ricorsiva (per saltare i livelli intermedi)
            var foundInChild = FindNodeWithHash(node.FirstChildIndex, targetHash, depthLimit - 1);
            if (foundInChild != 0) return foundInChild;

            current = node.NextSiblingIndex;
        }
        
        return 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RunIterations(int area, int counter = 0)
    {
        // Timeout dinamico: Puntiamo a 350ms per stare larghi nei 500ms totali
        const long maxTimeMs = 450;
        const long forcedMoveTimeMs = 50; // Se la mossa è forzata, spendiamo poco tempo per verificare
        
        var stopwatch = Stopwatch.StartNew();
        
        // Controlliamo se è una mossa forzata (1 sola scelta valida alla radice)
        // Espandiamo la radice una volta per vedere i figli
        if (_nodePool[_rootIndex].IsLeafNode)
        {
            _worker.RunIteration(area, _rootIndex); 
        }
        
        ref var rootNode = ref _nodePool[_rootIndex];
        var childCount = 0;
        var childIdx = rootNode.FirstChildIndex;
        while(childIdx != -1)
        {
            childCount++;
            childIdx = _nodePool[childIdx].NextSiblingIndex;
        }

        // Se abbiamo 1 sola mossa legale, non serve pensare troppo (a meno che non vogliamo vedere il futuro profondo per tie-breaking)
        var timeLimit = (childCount <= 1) ? forcedMoveTimeMs : maxTimeMs;

        while (stopwatch.ElapsedMilliseconds < timeLimit)
        {
            // Se la radice è risolta (Vittoria o Sconfitta certa), stop anticipato!
            if (rootNode.IsSolvedWin || rootNode.IsSolvedLoss) 
                break;
            
            // Esegui batch di iterazioni per ridurre l'overhead del controllo tempo
            for(var i=0; i<64; i++) 
            {
                _worker.RunIteration(area, _rootIndex);
            }
            counter += 64;
        }

        stopwatch.Stop();
    }
    
    public unsafe void GetRootStats(List<RootMoveStat> outputBuffer)
    {
        outputBuffer.Clear();

        if (_rootIndex <= 0) return;

        ref var rootNode = ref _nodePool[_rootIndex];
        var childIndex = rootNode.FirstChildIndex;

        while (childIndex != -1)
        {
            ref var childNode = ref _nodePool[childIndex];
            
            // Raccogliamo statistiche solo per le mosse valide del giocatore
            // Ignoriamo nodi risolti come persi se hanno 0 visite (a meno che non siano terminali forzati)
            if (childNode.Visits > 0 || childNode.IsSolvedWin)
            {
                // Calcoliamo uno score normalizzato per il debug
                // Nota: In MaxN childNode.Rewards[0] è il reward cumulativo.
                var avgScore = childNode.Visits > 0 ? childNode.Rewards[0] / childNode.Visits : -1;
                
                outputBuffer.Add(new RootMoveStat(childNode.Move, childNode.Visits, avgScore));
            }

            childIndex = childNode.NextSiblingIndex;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Reset()
    {
        _rootIndex = 0;
        _worker.Reset(1);
    }
    
    // Log methods rimosso per brevità, usare quello vecchio se serve debug
}