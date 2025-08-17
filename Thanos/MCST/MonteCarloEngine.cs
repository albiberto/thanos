using Thanos.Memory;
using Thanos.SourceGen;
using Thanos.War;

namespace Thanos.MCST;

public sealed unsafe class MonteCarloEngine(MemoryPool pool, in WarContext context, in MemoryLayout layout)
{
    private readonly WarContext _context = context;
    private readonly MemoryLayout _layout = layout;
    private Node* _root;

    /// <summary>
    /// Resetta l'albero di ricerca per una nuova posizione di partenza.
    /// </summary>
    /// <summary>
    /// Resetta l'albero di ricerca per una nuova posizione di partenza.
    /// </summary>
    public void Reset(in Request request)
    {
        pool.Reset();

        // La chiamata ora è più diretta: otteniamo subito ciò che ci serve.
        if (pool.TryGetNext(out var rootSlot))
        {
            // Non serve più creare 'rootSlot' manualmente.
            rootSlot.CloneFrom(in request);
            _root = rootSlot.GetNodePtr();
        }
        else
        {
            throw new OutOfMemoryException("Memory Pool is too small for the root node.");
        }
    }

    /// <summary>
    /// Esegue la ricerca MCTS e restituisce la mossa migliore (come bitmask).
    /// </summary>
    public byte FindBestMove(int iterations)
    {
        for (var i = 0; i < iterations; i++)
        {
            var leaf = Selection(_root);
            var expandedNode = Expansion(leaf);
            if (expandedNode == null) continue; 
            
            var result = Simulation(expandedNode);
            Backpropagation(expandedNode, result);
        }
        
        return GetBestMoveFromRoot();
    }
    
    // --- LE 4 FASI DI MCTS ---

    private Node* Selection(Node* node)
    {
        while (!node->IsLeaf)
        {
            var bestChild = node->GetBestChild();
            if (bestChild == null) return node; // Se non ci sono figli validi da selezionare, ci fermiamo qui.
            node = bestChild;
        }
        return node;
    }

    private Node* Expansion(Node* parentNode)
    {
        if (parentNode->IsTerminal) return parentNode;

        var parentSlot = pool.GetSlotFromPointer(parentNode);
        var parentArena = parentSlot.GetArena();

        // CORREZIONE 1: Memorizza 'Snakes' in una variabile locale stabile.
        var snakes = parentArena.Snakes;
        // Ora possiamo passare 'snakes[0]' per riferimento in modo sicuro.
        byte legalMoveSet = parentArena.GetLegalMoves(snakes[0]);

        if (legalMoveSet == Moves.None)
        {
            parentNode->SetTerminal();
            return parentNode;
        }

        // CORREZIONE 2: Sposta lo stackalloc fuori dal ciclo.
        scoped Span<byte> chosenMoves = stackalloc byte[_context.SnakeCount];

        foreach (byte move in Moves.AllDirections)
        {
            if ((legalMoveSet & move) != 0)
            {
                if (pool.TryGetNext(out var childSlot))
                {
                    childSlot.CloneFrom(in parentSlot);
                
                    var childArena = childSlot.GetArena();
                
                    // Riutilizza lo stesso span, cambiando solo i valori necessari.
                    chosenMoves.Fill(Moves.Up); // Mossa di default per gli avversari
                    chosenMoves[0] = move;      // Mossa del nostro serpente
                    childArena.SimulateTurn(chosenMoves);
                
                    parentNode->AddChild(childSlot.GetNodePtr(), move);
                }
            }
        }
    
        // Ritorna il primo figlio creato, o il genitore se l'allocazione è fallita e non ci sono figli.
        return parentNode->ChildrenCount > 0 ? (*parentNode)[0] : parentNode;
    }

    private float Simulation(Node* node)
    {
        var slot = pool.GetSlotFromPointer(node);
        var arena = slot.GetArena();

        // Alloca lo spazio per i set di mosse legali e per le mosse scelte
        scoped Span<byte> allLegalMoveSets = stackalloc byte[_context.SnakeCount];
        scoped Span<byte> chosenMoves = stackalloc byte[_context.SnakeCount];

        while (arena.Evaluate() == 0.0f)
        {
            var snakes = arena.Snakes;
    
            for (int i = 0; i < snakes.Length; i++)
            {
                var snake = snakes[i];
                if (snake.Dead)
                {
                    chosenMoves[i] = Moves.None;
                    continue;
                }

                byte legalMoveSet = arena.GetLegalMoves(snake);
        
                // --- INTEGRAZIONE QUI ---
                // PRIMA:
                // chosenMoves[i] = PickRandomMove(legalMoveSet);
        
                // DOPO:
                var finder = new HeuristicMoveFinder(ref snake, arena, legalMoveSet);
                chosenMoves[i] = finder.FindBestMove();
            }

            arena.SimulateTurn(chosenMoves);
        }

        return arena.Evaluate();
    }

    private void Backpropagation(Node* node, float result)
    {
        while (node != null)
        {
            node->Visits++;
            node->Wins += result;
            node = node->Parent;
        }
    }
    
    private byte GetBestMoveFromRoot()
    {
        long maxVisits = -1;
        var bestMove = Moves.Up;

        // CORREZIONE: Itera usando l'indexer del Node per evitare allocazioni.
        for (var i = 0; i < _root->ChildrenCount; i++)
        {
            var child = (*_root)[i]; // Usa l'indexer
            if (child->Visits > maxVisits)
            {
                maxVisits = child->Visits;
                bestMove = child->MoveThatLedToThisNode;
            }
        }
        return bestMove;
    }
}