using Thanos.Common;
using Thanos.Memory;
using Thanos.SourceGen;
using Thanos.War;

namespace Thanos.MCST;

public class MonteCarloEngine(WarMemoryPool warPool, NodeMemoryPool nodePool)
{
    private NodeMemoryPool _nodePool = nodePool;

    public byte FindBestMove(in Request request, int iterations = 10000)
    {
        var rootSlot = warPool.GetNext();
        rootSlot.InitializeFromRequest(in request);
        
        var rootIndex = _nodePool.GetNextIndex();
        ref var rootNode = ref _nodePool[rootIndex];
        
        rootNode.Initialize(parentIndex: -1, move: Moves.None);

        // 2. CICLO DI RICERCA MCTS
        for (var i = 0; i < iterations; i++)
        {
            // Clona lo stato della radice per iniziare ogni iterazione
            var workingSlot = warPool.GetNext();
            workingSlot.CloneFrom(in rootSlot);
            var arena = workingSlot.GetArena;
            
            // --- FASE 1: SELEZIONE ---
            var selectedNodeIndex = Select(rootIndex, arena);
            
            ref var selectedNode = ref _nodePool[selectedNodeIndex];

            // --- FASE 2: ESPANSIONE ---
            // Espandi se il nodo non è terminale e se non è già stato espanso prima
            if (!selectedNode.IsTerminal && selectedNode.IsLeafNode)
            {
                // Valuta lo stato PRIMA di espandere
                if (workingArena.Evaluate() == 0.0f) 
                {
                    Expand(selectedNodeIndex, ref selectedNode, ref workingArena);
                }
                else
                {
                    selectedNode.IsTerminal = true;
                }
            }
            
            // --- FASE 3: SIMULAZIONE (ROLLOUT) ---
            var simulationResult = Simulate(ref workingArena);
            
            // --- FASE 4: BACKPROPAGATION ---
            Backpropagate(selectedNodeIndex, simulationResult);
        }

        // 3. SCELTA DELLA MOSSA MIGLIORE
        // Finito il ciclo, scegliamo il figlio della radice più visitato
        var bestChildIndex = -1;
        var maxVisits = -1;

        ref var finalRootNode = ref _nodePool[rootIndex];
        foreach (var childIndex in finalRootNode.GetChildren(_nodePool))
        {
            ref var childNode = ref _nodePool[childIndex];
            if (childNode.Visits > maxVisits)
            {
                maxVisits = childNode.Visits;
                bestChildIndex = childIndex;
            }
        }
        
        return bestChildIndex != -1 ? _nodePool[bestChildIndex].MoveThatLedToThisNode : Moves.None;
    }

    /// <summary>
    /// FASE 1: Scende l'albero partendo da un indice, scegliendo i figli più promettenti (UCT)
    /// e aggiornando lo stato di gioco ('workingArena') di conseguenza.
    /// Restituisce l'indice del nodo foglia selezionato.
    /// </summary>
    private int Select(int startNodeIndex, ref WarArena arena) // <-- BUG #2 RISOLTO: aggiunto 'ref'
    {
        var currentIndex = startNodeIndex;

        while (true)
        {
            ref var currentNode = ref _nodePool[currentIndex];
            if (currentNode.IsLeafNode || currentNode.IsTerminal) return currentIndex;
        
            var nextNodeIndex = currentNode.SelectBestChild(_nodePool);

            // Non ci sono più figli da esplorare da questo ramo.
            // La selezione termina qui, restituendo il nodo attuale.
            if (nextNodeIndex == -1) return currentIndex;

            currentIndex = nextNodeIndex;
        
            ref var childNode = ref _nodePool[currentIndex];
            arena.ApplySingleMove(childNode.MoveThatLedToThisNode);
        }
    }

    /// <summary>
    /// FASE 2: Crea i nodi figli per un dato nodo foglia usando un ciclo bitwise diretto.
    /// </summary>
    private void Expand(int nodeIndex, ref Node node, ref WarArena arena)
    {
        if (node.IsTerminal) return;

        // 1. Ottieni la bitmask come prima
        var legalMovesMask = arena.Grid.GetLegalMoves(arena.Snakes.Me.Head);

        // Se non ci sono mosse, il nodo è di fatto un nodo terminale
        if (legalMovesMask == 0)
        {
            node.IsTerminal = true;
            return;
        }

        // 2. Cicla sulle possibili mosse, creando i figli man mano che le trovi
        var lastChildIndex = -1;
    
        // Per rendere il codice leggibile, iteriamo su un array di mosse possibili
        ReadOnlySpan<byte> allMoves = [Moves.Up, Moves.Down, Moves.Left, Moves.Right];

        foreach (var move in allMoves)
        {
            // Controlla se la mossa corrente è presente nella maschera
            if ((legalMovesMask & move) == 0) continue;
            
            // È una mossa legale, quindi creiamo il figlio
            var newChildIndex = _nodePool.GetNextIndex();
            ref var childNode = ref _nodePool[newChildIndex];
            childNode.Initialize(nodeIndex, move);

            // Ora dobbiamo collegare il nuovo figlio alla lista dei fratelli
            if (lastChildIndex == -1)
            {
                // Se è il primo figlio che troviamo, lo colleghiamo direttamente al genitore
                node.FirstChildIndex = newChildIndex;
            }
            else
            {
                // Altrimenti, lo colleghiamo al fratello precedente
                ref var lastChildNode = ref _nodePool[lastChildIndex];
                lastChildNode.NextSiblingIndex = newChildIndex;
            }
            
            // Aggiorniamo l'indice dell'ultimo figlio creato per il prossimo giro
            lastChildIndex = newChildIndex;
        }
    }

// All'interno della tua classe MonteCarloEngine

    /// <summary>
    /// FASE 3: Da uno stato di gioco, esegue una partita ("rollout") fino a un
    /// risultato terminale, restituendo il punteggio (-1 per sconfitta, 1 per vittoria).
    /// </summary>
    private float Simulate(ref WarArena arena)
    {
        // Limite di turni per evitare simulazioni infinite in caso di stallo
        const int turnLimit = 200; 

        for (var i = 0; i < turnLimit; i++)
        {
            // 1. Controlla se la partita è già terminata
            var evaluation = arena.Evaluate();
            if (evaluation != 0.0f)
            {
                return evaluation; // Ritorna -1.0f (sconfitta) o 1.0f (vittoria)
            }
        
            // 2. Ottieni le mosse legali come bitmask
            var legalMovesMask = arena.Grid.GetLegalMoves(arena.Snakes.Me.Head);
        
            // Se non ci sono mosse, è un pareggio (o una situazione di stallo)
            if (legalMovesMask == 0)
            {
                return 0.0f;
            }

            // 3. Scegli UNA mossa usando la tua euristica veloce
            var move = Heuristics.FindBestMove(legalMovesMask);
        
            // 4. Applica la mossa per far avanzare lo stato della simulazione
            arena.ApplySingleMove(move);
        }

        // Se la simulazione raggiunge il limite di turni, considerala un pareggio.
        return 0.0f;
    }

    /// <summary>
    /// FASE 4: Propaga il risultato della simulazione a ritroso lungo l'albero,
    /// aggiornando le statistiche (vittorie/visite) di ogni nodo attraversato.
    /// </summary>
    private void Backpropagate(int startNodeIndex, float result)
    {
        var currentIndex = startNodeIndex;
        while (currentIndex != -1)
        {
            ref var currentNode = ref _nodePool[currentIndex];
            currentNode.UpdateStats(result);
            
            // Risali al genitore per continuare la propagazione
            currentIndex = currentNode.ParentIndex;
        }
    }
}