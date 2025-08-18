using System.Diagnostics;
using System.Reflection.Metadata;
using Thanos.Enums;
using Thanos.Memory;
using Thanos.SourceGen;

namespace Thanos.MCST;

public sealed unsafe class MonteCarloEngine(MemoryPool pool)
{
    /// <summary>
    /// Esegue la ricerca MCTS e restituisce la mossa migliore (come bitmask).
    /// </summary>
    public byte FindBestMove(Node* root, in Request request)
    {
        var stopwatch = Stopwatch.StartNew();
    
        // Esegui il ciclo MCTS finché non siamo vicini al limite di tempo.
        while (stopwatch.ElapsedMilliseconds < request.Game.TimeLimit)
        {
            // Le 4 fasi rimangono identiche
            var leaf = Selection(root);
            var expandedNode = Expansion(in request.Board, leaf);
            if (expandedNode == null) continue; 
        
            var result = Simulation(in request.Board, expandedNode);
            Backpropagation(expandedNode, result);
        }
    
        stopwatch.Stop();
        // Utile per il debug: stampa quante iterazioni sei riuscito a fare nel tempo concesso
        // Console.WriteLine($"Iterazioni eseguite: {_root->Visits}");

        return GetBestMoveFromRoot(root);
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

    private Node* Expansion(in Board board, Node* parentNode)
    {
        if (parentNode->IsTerminal) return parentNode;

        var parentSlot = pool.GetSlotFromPointer(parentNode);
        var parentArena = parentSlot.GetArena(in board);
    
        // NOTA: 'ourSnakeIndex' dovrebbe essere determinato in modo robusto, non hardcodato a 0
        var ourSnakeIndex = 0; 
        var ourSnake = parentArena.Snakes[ourSnakeIndex];
        var legalMoveSet = parentArena.GetLegalMoves(ourSnake);

        if (legalMoveSet == Moves.None)
        {
            parentNode->SetTerminal();
            return parentNode;
        }

        // Per ogni nostra mossa legale...
        foreach (var move in Moves.AllDirections)
        {
            if ((legalMoveSet & move) != 0)
            {
                if (pool.TryGetNext(out var childSlot))
                {
                    // 1. Clona lo stato del genitore
                    childSlot.CloneFrom(in parentSlot);
                    var childArena = childSlot.GetArena(in board);
                
                    // 2. Applica SOLO la nostra mossa per creare il nuovo stato
                    // Questo è un metodo che dovrai creare in WarArena!
                    childArena.ApplySingleMove(ourSnakeIndex, move);
            
                    // 3. Aggiungi il figlio all'albero
                    parentNode->AddChild(childSlot.GetNodePtr(), move);
                }
            }
        }

        // Restituisce il primo nuovo figlio per la simulazione
        return parentNode->ChildrenCount > 0 ? (*parentNode)[0] : parentNode;
    }

    private float Simulation(in Board board, Node* node)
    {
        var slot = pool.GetSlotFromPointer(node);
        var arena = slot.GetArena(board);
    
        // CORREZIONE: Usa il conteggio dei serpenti preso direttamente dall'arena.
        scoped Span<byte> chosenMoves = stackalloc byte[arena.Snakes.Length];

        while (arena.Evaluate() == 0.0f)
        {
            var snakes = arena.Snakes;
            for (var i = 0; i < snakes.Length; i++)
            {
                var snake = snakes[i];
                if (snake.Dead)
                {
                    chosenMoves[i] = Moves.None;
                    continue;
                }

                var legalMoveSet = arena.GetLegalMoves(snake);
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
    
    private byte GetBestMoveFromRoot(Node* root)
    {
        long maxVisits = -1;
        var bestMove = Moves.Up;

        // CORREZIONE: Itera usando l'indexer del Node per evitare allocazioni.
        for (var i = 0; i < root->ChildrenCount; i++)
        {
            var child = (*root)[i]; // Usa l'indexer
            if (child->Visits > maxVisits)
            {
                maxVisits = child->Visits;
                bestMove = child->MoveThatLedToThisNode;
            }
        }
        return bestMove;
    }
}