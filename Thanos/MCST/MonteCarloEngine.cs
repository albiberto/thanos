using System.Diagnostics;
using Thanos.Memory;

namespace Thanos.MCST;

public sealed unsafe class MonteCarloEngine(MemoryPool pool)
{
    /// <summary>
    ///     Esegue la ricerca MCTS e restituisce la mossa migliore (come bitmask).
    /// </summary>
    public byte FindBestMove(Node* root, in GameContext context, int timeLimit)
    {
        var stopwatch = Stopwatch.StartNew();

        // Esegui il ciclo MCTS finché non siamo vicini al limite di tempo.
        while (stopwatch.ElapsedMilliseconds < timeLimit)
        {
            // Le 4 fasi rimangono identiche
            var leaf = Selection(root);
        
            // Se la selezione ci porta a un nodo terminale, non possiamo espandere.
            // Eseguiamo il backpropagation da qui.
            if (leaf->IsTerminal)
            {
                // Valuta lo stato terminale e propaga il risultato
                var terminalSlot = pool.GetSlotFromPointer(leaf);
                var terminalArena = terminalSlot.GetArena();
                Backpropagation(leaf, terminalArena.Evaluate());
                continue;
            }

            var expandedNode = Expansion(leaf, in context);
        
            // Se l'espansione fallisce o crea un nodo già terminale, passiamo al prossimo ciclo.
            if (expandedNode == null || expandedNode->IsTerminal) continue; 
    
            // CORREZIONE: Esegui la simulazione dal nuovo nodo espanso.
            var result = Simulation(expandedNode);
        
            // Esegui il backpropagation dal nuovo nodo.
            Backpropagation(expandedNode, result);
        }

        stopwatch.Stop();
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

    private Node* Expansion(Node* parentNode, in GameContext context)
    {
        if (parentNode->IsTerminal) return parentNode;

        var parentSlot = pool.GetSlotFromPointer(parentNode);
        var parentArena = parentSlot.GetArena();

        // --- Trova il nostro indice usando il context ---
        var snakes = parentArena.Snakes;
        var ourSnakeIndex = -1;
        for (var i = 0; i < snakes.Length; i++)
            // NOTA: Richiede di aggiungere il campo 'Id' a WarSnake
            if (snakes[i].index == context.MyId)
            {
                ourSnakeIndex = i;
                break;
            }

        // Se non ci troviamo o siamo morti, questo è un nodo terminale
        if (ourSnakeIndex == -1 || snakes[ourSnakeIndex].Dead)
        {
            parentNode->SetTerminal();
            return parentNode;
        }

        var ourSnake = snakes[ourSnakeIndex];
        var legalMoveSet = parentArena.GetLegalMoves(ourSnake);

        if (legalMoveSet == Moves.None)
        {
            parentNode->SetTerminal();
            return parentNode;
        }

        // Espandi creando un figlio per ogni mossa legale
        foreach (var move in Moves.AllDirections)
            if ((legalMoveSet & move) != 0)
                if (pool.TryGetNext(out var childSlot))
                {
                    childSlot.CloneFrom(in parentSlot);
                    var childArena = childSlot.GetArena();

                    // USA IL METODO CORRETTO: applica solo la nostra mossa
                    childArena.ApplySingleMove(ourSnakeIndex, move);

                    parentNode->AddChild(childSlot.GetNodePtr(), move);
                }

        return parentNode->ChildrenCount > 0 ? (*parentNode)[0] : parentNode;
    }

    /// <summary>
    ///     Esegue una simulazione ("playout" o "rollout") da un dato nodo fino alla fine del gioco.
    ///     Lavora su una copia dello stato per non modificare l'albero MCTS originale.
    /// </summary>
    /// <param name="node">Il nodo di partenza per la simulazione.</param>
    /// <returns>Il risultato della partita (1.0 vittoria, -1.0 sconfitta).</returns>
    private float Simulation(Node* node)
    {
        // 1. CLONAZIONE DELLO STATO
        // Per non modificare lo stato originale del nodo nell'albero, cloniamolo
        // in un nuovo slot di memoria temporaneo che useremo solo per questa simulazione.
        if (!pool.TryGetNext(out var simulationSlot)) return 0.0f; // Se non c'è memoria, considera la simulazione un pareggio.
        var sourceSlot = pool.GetSlotFromPointer(node);
        simulationSlot.CloneFrom(in sourceSlot);

        // Otteniamo la vista WarArena per il nostro stato temporaneo clonato.
        var arena = simulationSlot.GetArena();

        // 2. IL CICLO DI PLAYOUT
        // Continua a simulare turni finché la partita non è finita (vittoria o sconfitta).
        while (arena.Evaluate() == 0.0f)
        {
            var snakes = arena.Snakes;

            // Questo buffer è piccolo (max 8-12 elementi), quindi stackalloc è sicuro e veloce.
            scoped Span<byte> chosenMoves = stackalloc byte[snakes.Length];

            // 3. SCELTA DELLE MOSSE (EURISTICHE)
            // Per ogni serpente vivo, scegliamo una mossa "intelligente" usando l'euristica.
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

            // 4. AVANZAMENTO DEL TURNO
            // Esegui un singolo turno di gioco. Questo metodo usa il workspace
            // interno allo 'simulationSlot', garantendo thread-safety e zero-allocazioni.
            arena.SimulateTurn(chosenMoves);
        }

        // 5. RESTITUZIONE DEL RISULTATO
        // Quando il ciclo finisce, la partita è terminata. Restituiamo il risultato finale.
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