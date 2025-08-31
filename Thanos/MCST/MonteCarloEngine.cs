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
        // Se non stiamo riutilizzando un albero, non facciamo nulla. La radice verrà creata in FindBestMove.
        if (previousChosenNodeIndex == 0)
        {
            Reset();
            return;
        }

        long realStateHash = ZobristHasher.CalculateHash(in newTurnRequest, snakeIdMap);

        ref var parentNode = ref _nodePool[previousChosenNodeIndex];
        int newRootIndex = 0;
        
        // Cerca tra i "nipoti" se ce n'è uno che corrisponde allo stato reale
        foreach (var childIndex in parentNode.GetChildren(_nodePool))
        {
            ref var childNode = ref _nodePool[childIndex];
            if (childNode.StateHash == realStateHash)
            {
                newRootIndex = childIndex; // Trovato!
                break;
            }
        }

        if (newRootIndex != 0) // Cache Hit
        {
            _currentRootIndex = newRootIndex;
            ref var newRoot = ref _nodePool[_currentRootIndex];
            newRoot.ParentIndex = -1; // È la nuova radice
        }
        else // Cache Miss
        {
            Reset(); // Non abbiamo trovato una corrispondenza, resettiamo l'albero
        }
    }
}