using Thanos.Memory;
using Thanos.SourceGen;

namespace Thanos.MCST;

public sealed unsafe class MonteCarloEngine(MemoryPool pool)
{
    private Node* _root;
    
    /// <summary>
    /// Resetta l'albero di ricerca per una nuova posizione di partenza.
    /// </summary>
    public void Reset(in Request request)
    {
        // Resetta il puntatore del pool, rendendo tutta la memoria di nuovo disponibile
        pool.Reset();

        // Richiede il primo slot di memoria per il nodo radice
        if (pool.TryGetNext(out var rootSlot))
        {
            // Inizializza lo slot con lo stato di gioco iniziale
            rootSlot.CloneFrom(in request);
            
            // Memorizza il puntatore al nodo radice per iniziare le ricerche
            _root = rootSlot.GetNodePtr(); // Assumendo un nuovo helper in MemorySlot
        }
        else
        {
            // Gestisci l'errore: il pool non è abbastanza grande neanche per un nodo
            throw new OutOfMemoryException("Memory Pool is too small for the root node.");
        }
    }

    /// <summary>
    /// Esegue la ricerca MCTS per un numero fisso di iterazioni e restituisce la mossa migliore.
    /// </summary>
    public MoveDirection FindBestMove(int iterations)
    {
        for (var i = 0; i < iterations; i++)
        {
            // LE 4 FASI DI MCTS
            var leaf = Selection(_root);
            var expandedNode = Expansion(leaf);
            var result = Simulation(expandedNode);
            Backpropagation(expandedNode, result);
        }

        // Dopo le iterazioni, scegli la mossa che porta al figlio più visitato
        // (Logica da implementare)
        return GetBestMoveFromRoot();
    }
    
    // --- LE 4 FASI DI MCTS ---

    /// <summary>
    /// 1. SELEZIONE: Scende lungo l'albero scegliendo i nodi migliori fino a raggiungere una foglia.
    /// </summary>
    private Node* Selection(Node* node)
    {
        while (!node->IsLeaf) node = node->GetBestChild();
        return node;
    }

    /// <summary>
    /// 2. ESPANSIONE: Crea uno o più figli della foglia selezionata.
    /// </summary>
    private Node* Expansion(Node* node)
    {
        // Ottieni la vista MemorySlot e l'API WarArena per il nodo corrente
        var slot = pool.GetSlotFromPointer(node);
        var arena = slot.GetArena(); // Assumendo un nuovo helper in MemorySlot
        
        // Calcola le mosse legali da questo stato
        Span<MoveDirection> legalMoves = stackalloc MoveDirection[3];
        var moveCount = arena.GetLegalMoves(legalMoves);

        if (moveCount == 0) return node; // Nodo terminale, non si può espandere

        // Espandi creando un nuovo nodo figlio
        for (var i = 0; i < moveCount; i++)
        {
            if (pool.TryGetNext(out var childSlot))
            {
                // Clona lo stato del genitore nel nuovo slot
                childSlot.CloneFrom(slot); // Assumendo un CloneFrom che accetta MemorySlot

                // Ottieni l'arena del figlio e applica la mossa
                var childArena = childSlot.GetArena();
                childArena.SimulateTurn(legalMoves[i]);
                
                // Collega il figlio al genitore nell'albero (da implementare)
                node->AddChild(childSlot.GetNodePtr(), legalMoves[i]);
            }
        }
        
        // Per la simulazione, scegliamo il primo nuovo figlio creato
        return node->Child1;
    }

    /// <summary>
    /// 3. SIMULAZIONE (Rollout): Simula una partita casuale a partire dal nuovo nodo.
    /// </summary>
    private float Simulation(Node* node)
    {
        // Ottieni la vista MemorySlot e l'API WarArena per il nodo corrente
        var slot = pool.GetSlotFromPointer(node);
        var arena = slot.GetArena(); // Assumendo un nuovo helper in MemorySlot

        // Continua a fare mosse casuali finché la partita non finisce
        while (arena.Evaluate() == 0.0f)
        {
            Span<MoveDirection> legalMoves = stackalloc MoveDirection[3];
            var moveCount = arena.GetLegalMoves(legalMoves);
            if (moveCount == 0) break; // Partita finita

            var randomMove = legalMoves[Random.Shared.Next(moveCount)];
            arena.SimulateTurn(randomMove);
        }

        return arena.Evaluate();
    }

    /// <summary>
    /// 4. BACKPROPAGATION: Propaga il risultato della simulazione a ritroso lungo l'albero.
    /// </summary>
    private void Backpropagation(Node* node, double result)
    {
        while (node != null)
        {
            node->Visits++;
            node->Wins += result;
            node = node->Parent; // Risali al genitore
        }
    }
}