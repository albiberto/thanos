using System.Numerics;
using Thanos.Common;
using Thanos.Memory;
using Thanos.War;
using Thanos.War.Snake;

namespace Thanos.MCST;

public sealed class Worker(WarMemoryPool warPool, NodeMemoryPool nodePool)
{
    private static readonly byte[] AllMovesArray = [Moves.Up, Moves.Down, Moves.Left, Moves.Right];
    
    private readonly WarMemoryPool _warPool = warPool;
    private NodeMemoryPool _nodePool = nodePool;

    public void RunIteration(int rootNodeIndex, in MemorySlot rootSlot)
    {
        // 1. Setup
        var workingSlot = _warPool.GetNext();
        workingSlot.CloneFrom(in rootSlot);
        var arena = workingSlot.Arena;

        // 2. Selezione
        var leafNodeIndex = Select(rootNodeIndex, ref arena);
        ref var leafNode = ref _nodePool[leafNodeIndex];

        // 3. Espansione e Simulazione (Logica Unificata)
        if (arena.ILose)
        {
            leafNode.IsTerminal = true;
        }
        else if (leafNode.IsLeafNode)
        {
            // Espandi solo se è un nuovo nodo foglia
            Expand(leafNodeIndex, ref leafNode, in arena);
        }
    
        // Simula SEMPRE a meno che il nodo non sia già terminale dopo Select/Expand
        if (!leafNode.IsTerminal) 
        {
            Simulate(ref arena);
        }
    
        // La valutazione finale usa lo stato dell'arena DOPO la simulazione
        var simulationResult = workingSlot.Arena.Outcome();

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

            var nextNodeIndex = currentNode.SelectBestChild(_nodePool);
            if (nextNodeIndex == -1) return currentIndex;

            currentIndex = nextNodeIndex;
            ref var childNode = ref _nodePool[currentIndex];
            
            arena.ApplySingleMove(childNode.MoveThatLedToThisNode);
        }
    }

    private void Expand(int nodeIndex, ref Node node, in WarArena arena)
    {
        if (node.IsTerminal) return;

        var legalMovesMask = arena.GetLegalMoves();
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

    private static void Simulate(ref WarArena arena)
    {
        const int turnLimit = 100;

        for (var i = 0; i < turnLimit; i++)
        {
            var legalMovesMask = arena.GetLegalMoves();
            if (legalMovesMask == 0) return;
            
            var move = RolloutMove(legalMovesMask);
            
            arena.ApplySingleMove(move);
        }
    }
    
    private static byte RolloutMove(byte legalMoves)
    {
        if (legalMoves == 0) return Moves.Up; // Nessuna mossa legale, non dovrebbe succedere
        if (BitOperations.IsPow2(legalMoves)) return legalMoves; // Solo una mossa, prendi quella

        // Scegli una delle mosse legali a caso.
        var count = BitOperations.PopCount(legalMoves);
        var randomIndex = Random.Shared.Next(count);

        byte move = 0;
        while (randomIndex >= 0)
        {
            move = (byte)(1 << BitOperations.TrailingZeroCount(legalMoves));
            legalMoves &= (byte)~move;
            randomIndex--;
        }
        return move;
    }

    private void Backpropagate(int startNodeIndex, double rawScore)
    {
        // Un punteggio di 0 rimane 0.
        // Un punteggio molto alto (es. +200) si avvicina a +1.
        // Un punteggio molto basso (es. -200) si avvicina a -1.
        // Un punteggio basso (es. +10) diventa un valore intermedio (es. +0.2).
        // Questo "scalingFactor" controlla la sensibilità della curva. 
        // Un valore più basso rende la curva più ripida. Iniziamo con 100.
        const double scalingFactor = 100.0;
        var normalizedResult = Math.Tanh(rawScore / scalingFactor);

        var currentIndex = startNodeIndex;
        while (currentIndex != -1)
        {
            ref var currentNode = ref _nodePool[currentIndex];
            // Ora aggiorniamo le statistiche con un valore molto più informativo di un semplice +1 o -1
            currentNode.UpdateStats(normalizedResult);
            currentIndex = currentNode.ParentIndex;
        }
    }
}