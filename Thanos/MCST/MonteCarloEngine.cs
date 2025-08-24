using Thanos.Memory;
using Thanos.SourceGen;
using Thanos.War.Arena;

namespace Thanos.MCST;

public class MonteCarloEngine(WarMemoryPool pool)
{
    private Node _root;

    public byte FindBestMove(in Request request, int iterations = 10000)
    {
        // 1. Inizializza lo stato di partenza dalla richiesta
        var rootSlot = pool.GetNext();
        rootSlot.InitializeFromRequest(in request);
        
        _root = rootSlot.Node; 

        // 2. Esegui il ciclo di ricerca MCTS
        for (var i = 0; i < iterations; i++)
        {
            // Per ogni iterazione, partiamo sempre dallo stato originale della radice
            var workingSlot = pool.GetNext();
            workingSlot.CloneFrom(in rootSlot);
            
            var workingArena = workingSlot.GetArena;
            
            // --- FASE 1: SELEZIONE ---
            var selectedNode = Select(_root, ref workingArena);
            
            // --- FASE 2: ESPANSIONE ---
            // Espandiamo il nodo solo se non è un nodo terminale (partita finita)
            if (workingArena.Evaluate() == 0.0f)
            {
                Expand(selectedNode, ref workingArena);
            }
            
            // --- FASE 3: SIMULAZIONE ---
            // La simulazione parte dallo stato raggiunto dopo l'espansione
            float simulationResult = Simulate(ref workingArena);
            
            // --- FASE 4: BACKPROPAGATION ---
            Backpropagate(selectedNode, simulationResult);
        }

        // 3. Finito il ciclo, scegliamo la mossa del figlio più visitato
        var bestChild = _root.Children.MaxBy(c => c.Visits);
        return bestChild?.MoveThatLedToThisNode ?? Moves.None;
    }

    /// <summary>
    /// Scende l'albero scegliendo i nodi più promettenti e aggiorna lo stato dell'arena di conseguenza.
    /// </summary>
    private Node Select(Node node, ref WarArena arena)
    {
        while (!node.IsLeafNode)
        {
            node = node.SelectBestChild();
            if (node == null) break; // Non ci sono più mosse da esplorare da questo ramo
            
            // Applica la mossa all'arena per mantenerla sincronizzata con l'albero
            WarGameEngine.ApplySingleMove(ref arena, node.PlayerIndex, node.MoveThatLedToThisNode);
        }
        return node;
    }

    /// <summary>
    /// Crea i figli di un nodo foglia basandosi sulle mosse legali.
    /// </summary>
    private void Expand(Node node, ref WarArena arena)
    {
        // Chiedi all'arena le mosse legali per il giocatore corrente
        byte legalMoves = arena.GetLegalMoves(arena.GetSnake(node.PlayerIndex));
        
        // Chiedi al nodo di creare i suoi figli
        node.Expand(legalMoves);
    }

    /// <summary>
    /// Esegue una partita casuale ("rollout") fino a un risultato terminale.
    /// </summary>
    private float Simulate(ref WarArena arena)
    {
        int turnLimit = 200; // Limite di sicurezza
        for (int i = 0; i < turnLimit; i++)
        {
            float evaluation = arena.Evaluate();
            if (evaluation != 0.0f)
            {
                return evaluation; // Partita finita
            }
            
            // TODO: Gestire i turni per più giocatori
            var currentSnake = arena.GetSnake(0);
            
            // Usa una policy di default (es. euristica o casuale) per scegliere la mossa
            var heuristic = new HeuristicMoveFinder(ref currentSnake, arena);
            var legalMoves = arena.GetLegalMoves(currentSnake);
            var move = heuristic.FindBestMove(legalMoves);
            
            WarGameEngine.ApplySingleMove(ref arena, 0, move);
        }
        return 0.0f; // Pareggio per limite di turni
    }

    /// <summary>
    /// Propaga all'indietro il risultato della simulazione, aggiornando le statistiche dei nodi.
    /// </summary>
    private void Backpropagate(Node node, float result)
    {
        while (node != null)
        {
            node.UpdateStats(result);
            node = node.Parent;
        }
    }
}