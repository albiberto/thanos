using System.Diagnostics;
using Thanos.Common;
using Thanos.Memory;
using Thanos.SourceGen;

namespace Thanos.MCST;

public class MonteCarloEngine
{
    private readonly WarMemoryPool _warPool;
    private readonly NodeMemoryPool _nodePool;
    private readonly Worker _worker;
    
    // 1. L'indice della radice diventa un campo per mantenere lo stato tra i turni
    private int _currentRootIndex = 0; 

    public MonteCarloEngine(WarMemoryPool warPool, NodeMemoryPool nodePool)
    {
        _warPool = warPool;
        _nodePool = nodePool;
        _worker = new Worker(_warPool, _nodePool);
    }

    // 2. Il metodo ora restituisce l'INDICE del nodo scelto, non la mossa.
    // Questo è FONDAMENTALE per sapere quale nodo promuovere.
    public int FindBestMove(in Request request)
    {
        var slot = _warPool.GetNext(/* ... se hai aggiunto le Luts ... */);
        slot.InitializeFromRequest(in request);

        // 3. Se la radice non è inizializzata (inizio partita), creala.
        if (_currentRootIndex == 0)
        {
            _currentRootIndex = _nodePool.GetNextIndex();
            ref var root = ref _nodePool[_currentRootIndex];
            root.Initialize(-1, Moves.None);
        }

        var stopwatch = Stopwatch.StartNew();
        while (stopwatch.ElapsedMilliseconds < 450)
        {
            // Il worker lavora sempre sulla radice corrente
            _worker.RunIteration(_currentRootIndex, in slot);
        }
        
        ref var finalRootNode = ref _nodePool[_currentRootIndex];
        var bestChildIndex = finalRootNode.SelectMostVisitedChild(_nodePool);
        
        return bestChildIndex; // 4. Restituisce l'INDICE del figlio migliore
    }

    // 5. NUOVO METODO: L'Agent lo userà per dirci qual è la nuova radice
    public void SetNewRoot(int nodeIndex)
    {
        _currentRootIndex = nodeIndex;
        if (_currentRootIndex > 0)
        {
            // Questo è cruciale: il nuovo nodo radice non ha un genitore.
            ref var newRootNode = ref _nodePool[_currentRootIndex];
            newRootNode.ParentIndex = -1; 
        }
    }

    // 6. NUOVO METODO: L'Agent lo userà per resettare lo stato tra le partite
    public void Reset() => _currentRootIndex = 0;
    
    public void PrepareNextTurn(int previousChosenNodeIndex, in Request newTurnRequest, Dictionary<string, int> snakeIdMap)
    {
        // Se non c'è un albero precedente, riparti da zero.
        if (previousChosenNodeIndex == 0)
        {
            Reset();
            return;
        }

        // 1. Calcola l'hash dello stato REALE in cui ci troviamo ora.
        long realStateHash = ZobristHasher.CalculateHash(in newTurnRequest, snakeIdMap);

        // 2. Il nodo scelto al turno precedente (`previousChosenNodeIndex`) è la nostra
        //    nuova radice. Non dobbiamo cercare tra i suoi figli.
        //    Lo stato del gioco DOVREBBE corrispondere a questo nodo.
        ref var chosenNode = ref _nodePool[previousChosenNodeIndex];

        // 3. Verifica di coerenza (opzionale ma consigliata)
        // Se l'hash non corrisponde, qualcosa è andato storto nella simulazione vs realtà.
        // In questo caso, è più sicuro resettare tutto.
        if (chosenNode.StateHash != realStateHash)
        {
            // Cache Miss: la realtà non corrisponde alla nostra previsione. Resetta.
            Console.WriteLine("Tree Reuse Cache Miss! Resetting tree."); // Utile per il debug
            Reset(); 
            return;
        }

        // 4. Cache Hit! Promuovi il nodo scelto a nuova radice.
        _currentRootIndex = previousChosenNodeIndex;
        ref var newRoot = ref _nodePool[_currentRootIndex];
        newRoot.ParentIndex = -1; // Taglia il collegamento con il suo vecchio genitore.
    
        // Non è necessario fare il Clear/Reset del NodePool, perché stiamo riutilizzando
        // una porzione valida dell'albero. I nodi "vecchi" e non più raggiungibili
        // verranno semplicemente sovrascritti quando l'offset del pool avanzerà.
    }
}