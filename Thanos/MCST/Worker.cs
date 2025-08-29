using Thanos.Common;
using Thanos.Memory;
using Thanos.War;
using Thanos.War.Snake;

namespace Thanos.MCST;

public ref struct Worker(int rootNodeIndex, in MemorySlot rootSlot, WarMemoryPool warPool, NodeMemoryPool nodePool)
{
    // private static void LogSnakeBody(string prefix, in WarSnake snake)
    // {
    //     snake.GetSpans(out var first, out var second);
    //
    //     Console.Write($"{prefix} | Length: {snake.Length} | Segments: ");
    //
    //     foreach (var segment in first)
    //     {
    //         var x = segment % 11; // oppure snake.GridWidth
    //         var y = segment / 11;
    //         Console.Write($"({x},{y}) ");
    //     }
    //
    //     foreach (var segment in second)
    //     {
    //         var x = segment % 11;
    //         var y = segment / 11;
    //         Console.Write($"({x},{y}) ");
    //     }
    //
    //     Console.WriteLine();
    // }

    
    // --- CAMPI CORRETTI ---
    // Questi campi sono readonly perché vengono impostati una sola volta alla creazione.
    private readonly MemorySlot _rootSlot = rootSlot;

    private static readonly byte[] AllMovesArray = [Moves.Up, Moves.Down, Moves.Left, Moves.Right];

    // Stato dell'iterazione (l'unico campo non readonly)
    private MemorySlot _workingSlot;
    private NodeMemoryPool _nodePool = nodePool;

    // --- COSTRUTTORE CORRETTO ---
    // Riceve tutti i parametri e li assegna ai rispettivi campi.

    // --- METODO PRINCIPALE ---
    public void RunIteration()
    {
        // 1. Setup: Ora ha accesso a _warPool e _rootSlot
        _workingSlot = warPool.GetNext();
        _workingSlot.CloneFrom(in _rootSlot); 
        
        // LogSnakeBody("Initial", _workingSlot.General.Snakes.Me);
        
        var workingArena = _workingSlot.General;
        var scout = _workingSlot.Scout;

        // 2. Selezione: Ora ha accesso a _rootNodeIndex
        var leafNodeIndex = Select(rootNodeIndex, ref workingArena);
        ref var leafNode = ref _nodePool[leafNodeIndex];

        // 3. Espansione e Simulazione
        double simulationResult;
        if (workingArena.Snakes.Me.Dead)
        {
            leafNode.IsTerminal = true;
            simulationResult = scout.Evaluate();
        }
        else if (leafNode.IsLeafNode)
        {
            Expand(leafNodeIndex, ref leafNode, ref workingArena);
            Simulate(ref workingArena, in scout);
            simulationResult = _workingSlot.Scout.Evaluate();
        }
        else
        {
            Simulate(ref workingArena, in scout);
            simulationResult = _workingSlot.Scout.Evaluate();
        }

        // 4. Backpropagation
        Backpropagate(leafNodeIndex, simulationResult);
    }

    // --- FASI MCTS (Queste erano già corrette) ---
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
            arena.ApplySingleMove(childNode.MoveThatLedToThisNode);
        }
    }

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

    private void Simulate(ref General arena, in Scout scout)
    {
        const int turnLimit = 100;

        for (var i = 0; i < turnLimit; i++)
        {
            if (arena.Snakes.Me.Dead) return;

            var legalMovesMask = arena.Grid.GetLegalMoves(arena.Snakes.Me.Head);
            if (legalMovesMask == 0) return;
            
            var move = scout.SelectRolloutMove(legalMovesMask);
            
            // Console.WriteLine($"[Simulate] Step {i}, Move: {move}");
            // LogSnakeBody("BeforeMove", arena.Snakes.Me);

            arena.ApplySingleMove(move);

            // LogSnakeBody("AfterMove", arena.Snakes.Me);
        }
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