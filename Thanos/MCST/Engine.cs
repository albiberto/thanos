using Thanos.War;

namespace Thanos.MCST;

public unsafe class MctsEngine
{
    private readonly Random _random = new();
    
    // CORREZIONE: Passa lo stato iniziale per riferimento per evitare una copia massiva
    public MoveDirection FindBestMove(in WarArena rootState, WarContext* context)
    {
        // Crea una copia dello stato iniziale per il nodo radice, così non modifichiamo l'originale
        var rootArena = new WarArena(in rootState);
        var rootNode = new MctsNode(rootArena);

        // CORREZIONE: Garantisce la pulizia della memoria anche in caso di errori
        try
        {
            // Esegui l'algoritmo per un numero fisso di iterazioni
            for (var i = 0; i < 10000; i++)
            {
                var promisingNode = SelectPromisingNode(rootNode);

                // CORREZIONE: Controlla se il nodo è terminale
                if (!promisingNode.IsTerminalNode)
                {
                    ExpandNode(promisingNode, context);
                }
                
                var nodeToExplore = promisingNode;
                if (promisingNode.Children.Count > 0)
                {
                    nodeToExplore = promisingNode.Children[_random.Next(promisingNode.Children.Count)];
                }

                var result = SimulateRandomPlayout(nodeToExplore);
                Backpropagate(nodeToExplore, result);
            }

            // CORREZIONE: Trova il figlio migliore in modo più efficiente (senza LINQ)
            MctsNode? bestChild = null;
            var maxVisits = -1;
            foreach (var child in rootNode.Children)
            {
                if (child.Visits > maxVisits)
                {
                    maxVisits = child.Visits;
                    bestChild = child;
                }
            }
            
            return bestChild?.MoveThatLedToThisNode ?? MoveDirection.Up;
        }
        finally
        {
            // CORREZIONE: Libera la memoria dell'intero albero
            rootNode.Dispose();
        }
    }

    private MctsNode SelectPromisingNode(MctsNode node)
    {
        // TODO: Implementare la logica di selezione
        while (node.Children.Count > 0 && !node.IsTerminalNode)
        {
            node = node.FindBestChildUcb1() ?? node;
        }
        return node;
    }

    private void ExpandNode(MctsNode node, WarContext* context)
    {
        // TODO: Implementare la logica di espansione
        // Per ogni mossa legale da node.GameState:
        // 1. Crea una copia dello stato: var newState = new WarArena(in node.GameState);
        // 2. Simula la mossa: newState.Simulate(...);
        // 3. Crea il nodo figlio: var childNode = new MctsNode(newState, node, mossa);
        // 4. Aggiungi il figlio: node.Children.Add(childNode);
    }

    private double SimulateRandomPlayout(MctsNode node)
    {
        // TODO: Implementare la simulazione casuale (rollout)
        // 1. Crea una copia dello stato: var tempState = new WarArena(in node.GameState);
        // 2. In un ciclo, finché la partita non finisce, applica mosse casuali a tempState
        // 3. Restituisci il risultato (1.0 per vittoria, 0.0 per sconfitta, 0.5 per pareggio)
        // 4. Ricorda di fare il Dispose() di tempState alla fine!
        return _random.NextDouble();
    }

    private void Backpropagate(MctsNode node, double result)
    {
        // TODO: Implementare la propagazione all'indietro
        var tempNode = node;
        while (tempNode != null)
        {
            tempNode.UpdateStats(result);
            tempNode = tempNode.Parent;
        }
    }
}