using Thanos.Memory;
using Thanos.PreWarm.Memory;
using Thanos.War;

namespace Thanos.MCST;

public ref struct Worker(int rootNodeIndex, in MemorySlot rootSlot, WarMemoryPool warPool, NodeMemoryPool nodePool, in LutProvider lutProvider)
{
    private readonly NodeMemoryPool _nodePool = nodePool;
    private readonly WarMemoryPool _warPool = warPool;
    private readonly LutProvider _lutProvider = lutProvider;

    // Stato dell'iterazione corrente
    private MemorySlot _workingSlot; // Il nostro slot di memoria per la simulazione
    private int _currentNodeIndex;

    // Metodo che esegue una singola iterazione completa
    public void RunIteration()
    {
        // 1. Setup: Clona lo stato radice
        _workingSlot = _warPool.GetNext();
        _workingSlot.CloneFrom(in rootSlot);
        var workingArena = _workingSlot.GetArena;

        // 2. Selezione
        var leafNodeIndex = Select(rootNodeIndex, ref workingArena);
        ref var leafNode = ref _nodePool[leafNodeIndex];

        // 3. Espansione e Simulazione
        double simulationResult;
        if (workingArena.Snakes.Me.Dead)
        {
            leafNode.IsTerminal = true;
            simulationResult = Heuristics.Evaluate(ref workingArena, in _lutProvider);
        }
        else if (leafNode.IsLeafNode)
        {
            Expand(leafNodeIndex, ref leafNode, ref workingArena);
            // ... logica di simulazione ...
            simulationResult = Simulate(ref workingArena); // Simulate parte da workingArena già avanzata se c'è espansione
        }
        else
        {
            simulationResult = Simulate(ref workingArena);
        }

        // 4. Backpropagation
        Backpropagate(leafNodeIndex, simulationResult);
    }

    private int Select(int startNodeIndex, ref WarArena arena)
    {
        var currentIndex = startNodeIndex;
        while (true)
        {
            ref var currentNode = ref _nodePool[currentIndex];
            if (currentNode.IsLeafNode || currentNode.IsTerminal) return currentIndex;

            // <--- NOTA: Assicurati che SelectBestChild usi un fattore di esplorazione > 0
            var nextNodeIndex = currentNode.SelectBestChild(_nodePool);

            if (nextNodeIndex == -1) return currentIndex;

            currentIndex = nextNodeIndex;
            ref var childNode = ref _nodePool[currentIndex];
            arena.ApplySingleMove(childNode.MoveThatLedToThisNode);
        }
    }

    private void Expand(int nodeIndex, ref Node node, ref WarArena arena)
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

    private double Simulate(ref WarArena arena)
    {
        const int turnLimit = 100;

        for (var i = 0; i < turnLimit; i++)
        {
            if (arena.Snakes.Me.Dead) return double.NegativeInfinity;
            // if (arena.Snakes.LiveSnakesCount <= 1) return double.PositiveInfinity;

            var legalMovesMask = arena.Grid.GetLegalMoves(arena.Snakes.Me.Head);

            if (legalMovesMask == 0) return double.NegativeInfinity;

            var move = Heuristics.SelectRolloutMove(legalMovesMask, ref arena);

            arena.ApplySingleMove(move);
        }

        return Heuristics.Evaluate(ref arena, in _lutProvider);
    }

    private void Backpropagate(int startNodeIndex, double rawScore)
    {
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