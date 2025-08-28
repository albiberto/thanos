using Thanos.Common;
using Thanos.Memory;
using Thanos.War;

namespace Thanos.MCST;

public ref struct Worker(int rootNodeIndex, in MemorySlot rootSlot, WarMemoryPool warPool, NodeMemoryPool nodePool)
{
    // --- CAMPI ---
    // <--- CORREZIONE: Campo per l'indice del nodo radice

    private readonly MemorySlot _rootSlot = rootSlot; // Viene creata una copia dello slot radice
        // <--- CORREZIONE: Campo per lo stato di gioco alla radice

    
    private static readonly byte[] AllMovesArray = [Moves.Up, Moves.Down, Moves.Left, Moves.Right];

    // Stato dell'iterazione corrente
    private MemorySlot _workingSlot; // Il nostro slot di memoria per la simulazione
    private NodeMemoryPool _nodePool = nodePool;

    // --- Costruttore ---
    // Imposta tutti i campi readonly


    // --- METODO PRINCIPALE ---
    public void RunIteration()
    {
        // 1. Setup: Clona lo stato radice
        _workingSlot = warPool.GetNext();
        _workingSlot.CloneFrom(in _rootSlot); 
        
        // <--- CORREZIONE: Creiamo entrambe le viste, una per scrivere e una per leggere
        var workingArena = _workingSlot.General; // La nostra API di SCRITTURA (per modificare lo stato)
        var scout = _workingSlot.Scout;         // La nostra API di LETTURA (per valutare lo stato)

        // 2. Selezione (modifica lo stato di workingArena)
        var leafNodeIndex = Select(rootNodeIndex, ref workingArena);
        ref var leafNode = ref _nodePool[leafNodeIndex];

        // 3. Espansione e Simulazione
        double simulationResult;
        if (workingArena.Snakes.Me.Dead)
        {
            leafNode.IsTerminal = true;
            // <--- CORREZIONE: Usiamo lo Scout per la valutazione
            simulationResult = scout.Evaluate();
        }
        else if (leafNode.IsLeafNode)
        {
            // Espandiamo l'albero (non modifica workingArena, solo i nodi)
            Expand(leafNodeIndex, ref leafNode, ref workingArena);
            
            // Eseguiamo la simulazione (rollout), che modifica pesantemente workingArena
            Simulate(ref workingArena, in scout);
            
            // Valutiamo lo stato FINALE dopo la simulazione
            // <--- CORREZIONE: Creiamo un nuovo scout per lo stato finale di workingArena
            simulationResult = _workingSlot.Scout.Evaluate();
        }
        else // Il nodo non è una foglia, quindi simuliamo direttamente
        {
            Simulate(ref workingArena, in scout);
            // <--- CORREZIONE: Creiamo un nuovo scout per lo stato finale
            simulationResult = _workingSlot.Scout.Evaluate();
        }

        // 4. Backpropagation
        Backpropagate(leafNodeIndex, simulationResult);
    }

    // --- FASI MCTS ---
    private int Select(int startNodeIndex, ref General arena)
    {
        var currentIndex = startNodeIndex;
        while (true)
        {
            ref var currentNode = ref _nodePool[currentIndex];
            if (currentNode.IsLeafNode || currentNode.IsTerminal) return currentIndex;

            var nextNodeIndex = currentNode.SelectBestChild(_nodePool);
            if (nextNodeIndex == -1) return currentIndex;

            currentIndex = nextNodeIndex;
            ref var childNode = ref _nodePool[currentIndex];
            // L'arena viene MODIFICATA, quindi serve la vista General
            arena.ApplySingleMove(childNode.MoveThatLedToThisNode);
        }
    }

    // Questo metodo legge lo stato per decidere come creare i figli, quindi General va bene
    private void Expand(int nodeIndex, ref Node node, ref General arena)
    {
        if (node.IsTerminal) return;

        var legalMovesMask = arena.Grid.GetLegalMoves(arena.Snakes.Me.Head);
        if (legalMovesMask == 0)
        {
            node.IsTerminal = true;
            return;
        }

        var lastChildIndex = -1;

        foreach (var move in AllMovesArray)
        {
            if ((legalMovesMask & move) == 0) continue;

            var newChildIndex = _nodePool.GetNextIndex();
            ref var childNode = ref _nodePool[newChildIndex];
            childNode.Initialize(nodeIndex, move);

            if (lastChildIndex == -1)
            {
                node.FirstChildIndex = newChildIndex;
            }
            else
            {
                ref var lastChildNode = ref _nodePool[lastChildIndex];
                lastChildNode.NextSiblingIndex = newChildIndex;
            }

            lastChildIndex = newChildIndex;
        }
    }

    // <--- CORREZIONE: La firma del metodo ora accetta sia la vista di scrittura che quella di lettura
    private void Simulate(ref General arena, in Scout scout)
    {
        const int turnLimit = 100;

        for (var i = 0; i < turnLimit; i++)
        {
            if (arena.Snakes.Me.Dead) return;

            var legalMovesMask = arena.Grid.GetLegalMoves(arena.Snakes.Me.Head);
            if (legalMovesMask == 0) return;
            
            // <--- CORREZIONE: Usiamo lo Scout per SCEGLIERE la mossa (lettura)
            var move = scout.SelectRolloutMove(legalMovesMask);
            
            // <--- CORREZIONE: Usiamo l'Arena per APPLICARE la mossa (scrittura)
            arena.ApplySingleMove(move);
        }
        // <--- CORREZIONE: La valutazione finale viene fatta fuori da questo metodo
    }

    private void Backpropagate(int startNodeIndex, double rawScore)
    {
        // Math.Sign normalizza il risultato a +1 (vittoria), -1 (sconfitta), 0 (pareggio)
        var normalizedResult = (double)Math.Sign(rawScore);

        var currentIndex = startNodeIndex;
        while (currentIndex != -1)
        {
            ref var currentNode = ref _nodePool[currentIndex];
            currentNode.UpdateStats(normalizedResult);
            currentIndex = currentNode.ParentIndex;
        }
    }
}