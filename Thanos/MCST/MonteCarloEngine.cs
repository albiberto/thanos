using Thanos.Memory;
using Thanos.SourceGen;
using Thanos.War.Arena;

namespace Thanos.MCST;

public class MonteCarloEngine(MemoryPool pool)
{
    public Node Root { get; private set; }
    
    // Il metodo principale che l'agent chiamerà
    public byte FindBestMove(in Request request, int iterations = 10000)
    {
        var rootSlot = pool.GetNext();
        rootSlot.InitializeFromRequest(request);
        
        Root = rootSlot.GetNode(); 

        for (var i = 0; i < iterations; i++)
        {
            var slot = pool.GetNext();
            slot.CloneFrom(rootSlot); 
            var currentArena = slot.GetWarArena();
            
            // FASE 1: SELEZIONE
            var selectedNode = Select(Root);
            
            // FASE 2: ESPANSIONE
            var expandedNode = Expand(selectedNode, ref currentArena);
            
            // FASE 3: SIMULAZIONE
            var simulationResult = Simulate(ref currentArena);
            
            // FASE 4: BACKPROPAGATION
            Backpropagate(expandedNode, simulationResult);
        }

        // Finito il ciclo, scegliamo la mossa migliore (es. quella più visitata)
        return GetBestMoveFromRoot();
    }

    private Node Select(Node node)
    {
        // Logica per scegliere il figlio migliore usando la formula UCT/PUCT
        // Per ora, immaginiamo solo di scendere l'albero.
        while (node.HasChildren)
        {
            // API NECESSARIA: Dobbiamo applicare la mossa del nodo figlio all'arena.
            // arena.ApplySingleMove(node.BestChild.Move);
            node = node.BestChild; // Placeholder per la logica di selezione
        }
        return node;
    }

    private Node Expand(Node node, ref WarArena arena)
    {
        // API NECESSARIA: Chiediamo all'arena quali sono le mosse legali da questo stato.
        byte legalMoves = arena.GetLegalMoves(snakeIndex: 0); // snakeIndex del giocatore corrente

        // Creiamo i nodi figli, uno per ogni mossa legale.
        // ... logica per estrarre le singole mosse dalla maschera di bit ...
        // node.Children = ...;
        
        // Selezioniamo un nuovo figlio da cui partire per la simulazione (es. il primo)
        var childToExplore = node.Children[0];
        
        // API NECESSARIA: Applichiamo la mossa del nuovo figlio per far avanzare l'arena.
        arena.ApplySingleMove(childToExplore.Move, snakeIndex: 0);
        
        return childToExplore;
    }

    private float Simulate(ref WarArena arena)
    {
        // Eseguiamo mosse casuali (o basate su una policy semplice) fino a fine partita.
        while (true)
        {
            // API NECESSARIA: Controlliamo se la partita è finita.
            float evaluation = arena.Evaluate();
            if (evaluation != 0.0f) // 1.0 per vittoria, -1.0 per sconfitta
            {
                return evaluation;
            }

            // API NECESSARIA: Chiediamo le mosse legali per il giocatore di turno.
            byte legalMoves = arena.GetLegalMoves(arena.CurrentPlayerIndex);
            
            // Scegliamo una mossa a caso tra quelle legali
            var randomMove = Heuristics.FindBestMove(legalMoves);

            // API NECESSARIA: Applichiamo la mossa scelta.
            arena.ApplySingleMove(randomMove, arena.CurrentPlayerIndex);
        }
    }

    private void Backpropagate(Node node, float result)
    {
        // Risaliamo l'albero fino alla radice aggiornando le statistiche.
        while (node != null)
        {
            node.Visits++;
            node.Wins += result; // Aggiustare il risultato in base al giocatore del nodo
            node = node.Parent;
        }
    }
    
    private byte GetBestMoveFromRoot() { /* ... */ return Moves.Up; }
}