using System.Numerics;
using Thanos.Common;
using Thanos.Memory;
using Thanos.War;

namespace Thanos.MCST;

public sealed class Worker
{
    private static readonly byte[] AllMovesArray = [Moves.Up, Moves.Down, Moves.Left, Moves.Right];
    private readonly NodeMemoryPool _nodePool;

    private readonly SlotMemoryPool _slotPool;

    private int _nextId;

    // Il costruttore non ha bisogno di essere una expression body per chiarezza
    public Worker(SlotMemoryPool slotPool, NodeMemoryPool nodePool)
    {
        _slotPool = slotPool;
        _nodePool = nodePool;

        _nextId = 0;
    }

    private int AllocateNextId() => _nextId++;

    public void RunIteration(int rootIndex)
    {
        // 1. SELECTION - Scende nell'albero fino a trovare un nodo foglia.
        var leafIndex = Select(rootIndex);

        Expand(leafIndex);
    }

    private int Select(int startNodeIndex)
    {
        var currentIndex = startNodeIndex;

        while (true)
        {
            ref var currentNode = ref _nodePool[currentIndex];

            // Condizione di terminazione: siamo arrivati a una foglia o a un nodo terminale.
            if (currentNode.IsLeafNode || currentNode.IsTerminal) return currentIndex;

            // 1. TROVA: il miglior figlio del nodo CORRENTE
            var nextNodeIndex = SelectBestChild(ref currentNode);

            if (nextNodeIndex == -1) throw new InvalidOperationException("SelectBestChild ha restituito -1 in un nodo non foglia.");

            // 2. AGGIORNA: l'indice e lascia che il ciclo continui per scendere al livello successivo
            currentIndex = nextNodeIndex;
        }
    }

    private int SelectBestChild(ref Node node, double explorationParameter = 1.41)
    {
        var bestScore = double.MinValue;
        var bestChildIndex = -1;

        var logParentVisits = Math.Log(node.Visits);

        var childIndex = node.FirstChildIndex;
        while (childIndex != -1)
        {
            ref var childNode = ref _nodePool[childIndex];

            if (childNode.Visits == 0) return childIndex;

            var exploitation = childNode.Wins / childNode.Visits;
            var exploration = Math.Sqrt(logParentVisits / childNode.Visits);
            var uctScore = exploitation + explorationParameter * exploration;

            if (uctScore > bestScore)
            {
                bestScore = uctScore;
                bestChildIndex = childIndex;
            }

            childIndex = childNode.NextSiblingIndex;
        }

        return bestChildIndex;
    }

    private void Expand(int parentNodeIndex)
    {
        ref var parentNode = ref _nodePool[parentNodeIndex];
        var parentSlot = _slotPool[parentNodeIndex]; // Legame implicito!
        var parentArena = parentSlot.Arena;

        // 2. CONTROLLI PRELIMINARI
        if (parentArena.GameOver)
        {
            parentNode.IsTerminal = true;
            return;
        }

        // 3. CALCOLA LE MOSSE POSSIBILI
        var legalMoves = parentArena.GetLegalMoves();
        if (legalMoves == 0)
        {
            parentNode.IsTerminal = true;
            return;
        }

        // 4. CREA I NODI FIGLI
        var lastChildIndex = -1;
        foreach (var move in AllMovesArray)
        {
            if ((legalMoves & move) == 0) continue;

            // --- Alloca un INDEX unificato per il nuovo figlio ---
            var childIndex = AllocateNextId();

            // --- a. Usa INDEX per preparare lo stato del figlio ---
            var childSlot = _slotPool[childIndex];
            childSlot.CloneFrom(in parentSlot);
            var arena = childSlot.Arena;
            arena.ApplySingleMove(move);

            var hash = ZobristHasher.CalculateHash(in arena);

            // --- b. Usa LO STESSO INDEX per preparare il nodo del figlio ---
            ref var childNode = ref _nodePool[childIndex];
            childNode.Initialize(parentNodeIndex, move, hash);

            // --- c. Collega il nuovo figlio all'albero ---
            if (lastChildIndex == -1)
            {
                parentNode.FirstChildIndex = childIndex;
            }
            else
            {
                ref var lastChildNode = ref _nodePool[lastChildIndex];
                lastChildNode.NextSiblingIndex = childIndex;
            }

            lastChildIndex = childIndex;
        }
    }


    private static void Simulate(ref WarArena arena)
    {
        const int turnLimit = 100;
        for (var i = 0; i < turnLimit; i++)
        {
            if (arena.Outcome() != 0.0f) return;
            var legalMovesMask = arena.GetLegalMoves();
            if (legalMovesMask == 0) return;
            var move = RolloutMoveRandom(legalMovesMask);
            arena.ApplySingleMove(move);
        }
    }

    private static byte RolloutMoveRandom(byte legalMoves)
    {
        if (legalMoves == 0) return Moves.Up;
        if (BitOperations.IsPow2(legalMoves)) return legalMoves;

        var count = BitOperations.PopCount(legalMoves);
        var randomIndex = Random.Shared.Next(count);

        byte move = 0;
        for (var i = 0; i <= randomIndex; i++)
        {
            move = (byte)(1 << BitOperations.TrailingZeroCount(legalMoves));
            legalMoves &= (byte)~move;
        }

        return move;
    }

    private void Backpropagate(int startNodeIndex, double rawScore)
    {
        const double scalingFactor = 100.0;
        var normalizedResult = Math.Tanh(rawScore / scalingFactor);

        var currentIndex = startNodeIndex;
        while (currentIndex != -1)
        {
            ref var currentNode = ref _nodePool[currentIndex];
            currentNode.UpdateStats(normalizedResult);
            currentIndex = currentNode.ParentIndex;
        }
    }

    public void Reset(int newRootIndex) => _nextId = newRootIndex;
}