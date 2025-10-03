using System.Diagnostics;
using Thanos.Common;
using Thanos.Memory;
using Thanos.SourceGen;

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
            _worker.Reset(1, request.Game.Ruleset.Settings);

            var rootArena = _slotPool.GetArena(1);
            rootArena.InitializeFromRequest(in request);

            _rootHash = ZobristHasher.CalculateHash(rootArena);

            ref var rootNode = ref _nodePool[1]; // Usa l'indice 0
            rootNode.PlacementRoot(-1, Moves.None, _rootHash);

            _rootIndex = 1;
        }
        else
        {
            // Se siamo qui, PrepareNextTurn ha funzionato!
            // La radice è già impostata. Dobbiamo solo aggiornare il suo stato
            // con i dati reali della richiesta, perché quello attuale è simulato.
            var rootSlot = _slotPool.GetArena(_rootIndex);
            rootSlot.InitializeFromRequest(in request);
        }

        var stopwatch = Stopwatch.StartNew();
        var counter = 0;
        while (stopwatch.ElapsedMilliseconds < 450) // Limite di tempo per l'iterazione
            // while (counter < 10000)
        {
            _worker.RunIteration(_rootIndex);
            counter++;
        }

        Console.WriteLine($"[MCE] Iterazioni completate: {counter}");

        ref var finalRootNode = ref _nodePool[_rootIndex];

        var bestChildIndex = finalRootNode.SelectMostVisitedChild(_nodePool);
        return bestChildIndex;
    }

    private void Reset()
    {
        _rootIndex = 0;
        _worker.Reset(1); // Resetta il worker per iniziare ad allocare dal prossimo ID disponibile.
    }

    /// <summary>
    ///    Tenta di trovare un nodo nell'albero precedente che corrisponda allo stato attuale.
    ///    Se lo trova, lo promuove a nuova radice; altrimenti, resetta l'albero.
    /// </summary>
    /// <returns>True se l'albero è stato riutilizzato, false se è stato resettato.</returns>
    public bool PrepareNextTurn(int lastChosenIndex, long currentBoardHash)
    {
        // Ora il metodo può operare correttamente sul nodo corretto
        // se lastChosenIndex è un valore valido.

        // Se non abbiamo una radice precedente, non possiamo riutilizzare nulla.
        if (lastChosenIndex == 0) return false;

        // Cerca un figlio della vecchia radice che abbia l'hash dello stato corrente.
        var childIndex = _nodePool[_rootIndex].FirstChildIndex;
        while (childIndex != -1)
        {
            ref var childNode = ref _nodePool[childIndex];
            if (childNode.Hash == currentBoardHash)
            {
                // Trovato! Promuoviamo questo nodo a nuova radice.
                _rootIndex = childIndex;
                ref var newRoot = ref _nodePool[_rootIndex];

                // Rimuoviamo il genitore per segnalare che ora è la radice.
                newRoot.ParentIndex = -1;

                // Resetta esplicitamente la generazione a 0.
                // Questa è la modifica più importante per la logica di riutilizzo.
                newRoot.Generation = 0;

                // Comunichiamo al worker il nuovo ID di partenza per le allocazioni.
                // Questa è la parte più complessa e richiede una scansione dell'albero per trovare il maxID.
                // Per un'implementazione semplice, puoi continuare a contare sequenzialmente,
                // accettando un po' di spreco di memoria. Un'opzione migliore è mantenere un conteggio
                // massimo o cercare l'ID più alto, ma questo rallenterebbe il turno.
                _worker.Reset(_worker.GetMaxId(_rootIndex) + 1);
                // _worker.Reset(_worker.GetNextId()); // Opzione più semplice (ma meno efficiente)

                // Aggiorniamo anche il nostro hash di radice per il prossimo controllo.
                _rootHash = currentBoardHash;

                return true;
            }

            childIndex = childNode.NextSiblingIndex;
        }

        // Hash non corrispondente o nodo non trovato, quindi resetta.
        Reset();
        return false;
    }
}