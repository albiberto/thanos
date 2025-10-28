using System.Diagnostics;
using System.Runtime.CompilerServices;
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
        // --- 1. LOGICA DI RIUTILIZZO DELL'ALBERO ---
        if (previousMoveIndex > 0)
        {
            // Tenta di trovare un nuovo nodo radice in base alla mossa dell'avversario
            _rootIndex = FindNewRoot(previousMoveIndex, in request);

            if (_rootIndex > 0)
            {
                #if DEBUG
                Console.WriteLine($"[Engine.FindBestMove] INFO: Riutilizzo albero riuscito. Nuova radice: {_rootIndex}.");
                #endif
                
                // Imposta il nodo trovato come nuova radice
                ref var newRootNode = ref _nodePool[_rootIndex];
                newRootNode.NewRoot(); // Imposta ParentIndex = -1
                
                // Sincronizza lo stato dell'arena di questo nodo con il request attuale
                // (es. per aggiornare cibo, salute, ecc.)
                var rootArena = _slotPool.GetArena(_rootIndex);
                rootArena.InitializeFromRequest(in request);
            }
        }
        else
        {
            _rootIndex = 0; // Forza un reset se non c'è una mossa precedente
        }
        
        // --- 2. CREAZIONE NUOVO ALBERO (se _rootIndex è 0) ---
        // Questo blocco ora gestisce sia l'inizio di una partita (previousMoveIndex == 0)
        // sia un fallimento nel riutilizzo dell'albero (_rootIndex == 0)
        if (_rootIndex == 0)
        {
            #if DEBUG
                if (previousMoveIndex > 0) {
                    Console.WriteLine("[Engine.FindBestMove] INFO: Riutilizzo albero fallito. Creazione nuovo albero.");
                } else {
                    Console.WriteLine("[Engine.FindBestMove] INFO: Creazione nuovo albero MCTS da zero.");
                }
            #endif

            _rootIndex = 1; // Usa lo slot 1 come radice
            _worker.Reset(_rootIndex, request.Game.Ruleset.Settings);

            // Calcola l'hash per lo stato ATTUALE
            var hash = _slotPool.CalculateHash(_rootIndex, in request);
            ref var rootNode = ref _nodePool[_rootIndex];
            rootNode.PlacementRoot(hash);
            
            // INIZIALIZZA anche l'arena per la nuova radice
            var rootArena = _slotPool.GetArena(_rootIndex);
            rootArena.InitializeFromRequest(in request);
        }

        // --- 3. ESECUZIONE ITERAZIONI ---
        // _rootIndex è ora impostato correttamente sul nodo che rappresenta
        // lo stato attuale, ed è il *nostro* turno (Player 0).
        RunIterations(request.Board.Area);
        
        // --- 4. SELEZIONE MOSSA ---
        #if DEBUG
            var bestChildIndex = _nodePool.SelectMostVisitedChildWithLogging(_rootIndex);
        #else
            var bestChildIndex = _nodePool.SelectMostVisitedChild(_rootIndex);
        #endif

        // Restituisce l'indice del nodo della *nostra* mossa migliore
        return bestChildIndex;
    }

    private int FindNewRoot(int myLastMoveNodeIndex, in Request request)
    {
        // 1. Calcola l'hash dello stato attuale (Turno 9).
        //    Usa temporaneamente lo slot 1 per questo calcolo.
        var currentHash = _slotPool.CalculateHash(1, in request);

        // 2. Prendi il nodo della nostra mossa precedente (Node 1127).
        //    I suoi figli rappresentano le possibili risposte dell'avversario
        //    simulate durante il Turno 8.
        ref var myLastMoveNode = ref _nodePool[myLastMoveNodeIndex];
        if (myLastMoveNode.IsLeafNode)
        {
            return 0; // L'avversario non è stato simulato, resetta.
        }

        // 3. Itera attraverso i figli (le mosse simulate dell'avversario).
        var childIndex = myLastMoveNode.FirstChildIndex;
        while (childIndex != -1)
        {
            ref var childNode = ref _nodePool[childIndex];
            
            // 4. Confronta l'hash del nodo figlio con l'hash dello stato attuale.
            if (childNode.Hash == currentHash)
            {
                // TROVATO! Questo figlio rappresenta la mossa che l'avversario
                // ha effettivamente fatto. Diventerà la nostra nuova radice.
                // (es. Node 1200, che ha PlayerIndex = 0)
                return childIndex;
            }
            
            childIndex = childNode.NextSiblingIndex;
        }

        // 5. Nessuna corrispondenza. L'avversario ha fatto una mossa che non avevamo
        //    esplorato, o c'è un problema di hash. Resettiamo l'albero.
        return 0;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RunIterations(int area, int counter = 0)
    {
        var stopwatch = Stopwatch.StartNew();

        // while (stopwatch.ElapsedMilliseconds < 10000000000)
        while (counter < 50)
        {
            _worker.RunIteration(area, _rootIndex);
            counter++;
        }
        
        stopwatch.Stop();
        
        #if DEBUG
            Console.WriteLine($"[Engine.FindBestMove.RunIterations] INFO: Iterations completed: {counter} in {stopwatch.ElapsedMilliseconds}ms.");
        #endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Reset()
    {
        Console.WriteLine("[Engine.Reset] INFO: Resettando l'albero MCTS.");
        _rootIndex = 0;
        _worker.Reset(1);
    }
}