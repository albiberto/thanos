using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using Thanos.Abstract;
using Thanos.Common;
using Thanos.SourceGen;
using Thanos.War;

namespace Thanos.MCST;

public sealed class Worker(ISlotMemoryPool slotPool, INodeMemoryPool nodeMemoryPool) : IWorker
{
    // PARAMETRI "HIVE MIND"
    private const double EXPLORATION_PARAMETER = 1.41;
    private const int FIXED_POINT_FACTOR = 10000; // Scala per Interlocked.Add
    private const int VIRTUAL_LOSS = 1; // Penalità visite per concorrenza

    private RulesetSettings _settings;

    private readonly INodeMemoryPool _nodeMemoryPool = nodeMemoryPool;
    private readonly ISlotMemoryPool _slotPool = slotPool;

    private static readonly byte[] AllMoves = [Moves.Up, Moves.Down, Moves.Left, Moves.Right];
    
    // Buffer locali (Thread-Safe per istanza di Worker)
    private readonly float[] _rewardsBuffer = new float[Constants.MaxSnakesCount];
    private readonly int[] _atomicRewardsBuffer = new int[Constants.MaxSnakesCount]; 

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RunIteration(int area, int rootIndex)
    {
        // 1. SELECTION (con Virtual Loss)
        // Scende l'albero e applica la penalità ai nodi visitati per scoraggiare altri thread.
        var leafIndex = Select(rootIndex);
        if (leafIndex == -1) return;

        ref var leafNode = ref _nodeMemoryPool.Get(leafIndex);

        // 2. EXPANSION
        // Se non è terminale/risolto, generiamo i figli (Stati Futuri Simultanei)
        if (leafNode.IsLeafNode && !leafNode.IsTerminal && !leafNode.IsSolvedWin && !leafNode.IsSolvedLoss) 
        {
            ExpandSimultaneousMoves(leafIndex, ref leafNode, area);
        }

        // 3. EVALUATION
        // Se abbiamo espanso, valutiamo il primo figlio (Best Guess), altrimenti il nodo stesso.
        var nodeToEvaluate = leafIndex;
        if (!leafNode.IsLeafNode) nodeToEvaluate = leafNode.FirstChildIndex;

        Evaluate(nodeToEvaluate, _rewardsBuffer);

        // 4. BACKPROPAGATION
        // Risale l'albero, aggiorna le statistiche e rimuove la Virtual Loss.
        Backpropagate(nodeToEvaluate, leafIndex, _rewardsBuffer);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int Select(int rootIndex)
    {
        var currentIndex = rootIndex;
        var depth = 0; 
        const int maxDepth = 1000; 

        while (depth++ < maxDepth)
        {
            ref var currentNode = ref _nodeMemoryPool.Get(currentIndex);

            if (currentNode.IsLeafNode || currentNode.IsTerminal || currentNode.IsSolvedWin || currentNode.IsSolvedLoss)
                return currentIndex;

            var bestChild = SelectBestChildMaxN(ref currentNode);
            if (bestChild == -1)
            {
                currentNode.MarkTerminal();
                return currentIndex;
            }

            // --- CONCURRENCY CONTROL ---
            // Applichiamo Virtual Loss al figlio scelto per "prenotarlo"
            ref var childNode = ref _nodeMemoryPool.Get(bestChild);
            Interlocked.Add(ref childNode.VirtualLoss, VIRTUAL_LOSS);

            currentIndex = bestChild;
        }
        
        return currentIndex;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe int SelectBestChildMaxN(ref Node parentNode)
    {
        var bestScore = double.MinValue;
        var bestChildIndex = -1;
        var logParentVisits = Math.Log(parentNode.Visits + 1); 
        var playerIndex = 0; // Ottimizziamo sempre per Hero (Index 0)

        var childIndex = parentNode.FirstChildIndex;
        while (childIndex != -1)
        {
            ref var childNode = ref _nodeMemoryPool.Get(childIndex);

            if (childNode.IsSolvedWin) return childIndex;
            
            if (childNode.IsSolvedLoss)
            {
                childIndex = childNode.NextSiblingIndex;
                continue;
            }

            // UCT con VIRTUAL LOSS
            // Effective Visits = Reali + Virtuali (penalizza denominatore)
            var effectiveVisits = childNode.Visits + childNode.VirtualLoss;

            if (effectiveVisits == 0) return childIndex; // Priorità inesplorati

            // Recuperiamo lo score atomico (Fixed-Point)
            var currentSum = childNode.AtomicRewards[playerIndex]; 
            
            // Penalità Virtuale sul numeratore (Assume che l'esplorazione concorrente sia una "sconfitta" temporanea)
            // Score range [-1..1]. Loss = -1.
            var virtualPenalty = childNode.VirtualLoss * FIXED_POINT_FACTOR; 
            
            var adjustedSum = currentSum - virtualPenalty;
            var averageScore = (double)adjustedSum / effectiveVisits / FIXED_POINT_FACTOR;

            var exploration = EXPLORATION_PARAMETER * Math.Sqrt(logParentVisits / effectiveVisits);
            var uctScore = averageScore + exploration;

            if (uctScore > bestScore)
            {
                bestScore = uctScore;
                bestChildIndex = childIndex;
            }

            childIndex = childNode.NextSiblingIndex;
        }

        if (bestChildIndex == -1 && parentNode.FirstChildIndex != -1)
        {
             return parentNode.FirstChildIndex;
        }

        return bestChildIndex;
    }

    // --- EXPANSION LOGIC (SIMULTANEOUS) ---

    private void ExpandSimultaneousMoves(int parentIndex, ref Node parentNode, int area)
    {
        var parentArena = _slotPool.GetArena(parentIndex);
        var snakeCount = parentArena.System.Count;

        // Check rapido morte Hero
        if (parentArena.System[0].IsDead) 
        {
            parentNode.MarkTerminal();
            parentNode.MarkSolvedLoss();
            return;
        }

        // Calcolo mosse plausibili (Bitmasks)
        Span<byte> movesMasks = stackalloc byte[snakeCount];
        for (var i = 0; i < snakeCount; i++)
        {
            if (parentArena.System[i].IsDead)
            {
                movesMasks[i] = 0;
            }
            else
            {
                var mask = parentArena.GetPlausibleMoves(i);
                if (mask == 0) 
                {
                    if (i == 0) // Hero non ha mosse -> Game Over
                    {
                        parentNode.MarkTerminal();
                        parentNode.MarkSolvedLoss();
                        return;
                    }
                    movesMasks[i] = 0; 
                }
                else
                {
                    movesMasks[i] = mask;
                }
            }
        }

        // Generazione Combinatoria
        Span<byte> currentMovesBuffer = stackalloc byte[snakeCount];
        var lastChildIndex = -1;

        GenerateCombinationsAndExpand(0, snakeCount, movesMasks, currentMovesBuffer, parentIndex, ref lastChildIndex, in parentArena);

        if (lastChildIndex == -1)
        {
            parentNode.MarkTerminal();
        }
    }

    private void GenerateCombinationsAndExpand(
        int currentSnakeIndex, 
        int totalSnakes, 
        ReadOnlySpan<byte> movesMasks, 
        Span<byte> currentMoves, 
        int parentIndex,
        ref int lastChildIndex,
        in Arena parentArena)
    {
        if (currentSnakeIndex == totalSnakes)
        {
            CreateChildNode(currentMoves, parentIndex, ref lastChildIndex, in parentArena);
            return;
        }

        var mask = movesMasks[currentSnakeIndex];

        if (mask == 0) 
        {
            currentMoves[currentSnakeIndex] = Moves.None;
            GenerateCombinationsAndExpand(currentSnakeIndex + 1, totalSnakes, movesMasks, currentMoves, parentIndex, ref lastChildIndex, in parentArena);
        }
        else
        {
            foreach (var move in AllMoves)
            {
                if ((mask & move) != 0)
                {
                    currentMoves[currentSnakeIndex] = move;
                    GenerateCombinationsAndExpand(currentSnakeIndex + 1, totalSnakes, movesMasks, currentMoves, parentIndex, ref lastChildIndex, in parentArena);
                }
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void CreateChildNode(
        ReadOnlySpan<byte> moves, 
        int parentIndex, 
        ref int lastChildIndex, 
        in Arena parentArena)
    {
        var childIndex = _nodeMemoryPool.Allocate(); // Thread-Safe Atomic Allocation
        var childSlotIndex = _slotPool.Allocate();

        if (childIndex == -1 || childSlotIndex == -1) return;

        var childArena = _slotPool.GetArena(childIndex);
        childArena.CloneFrom(in parentArena);

        // Simulazione Simultanea
        childArena.SimulateTurn(moves, _settings.HazardDamagePerTurn);

        var hash = ZobristHasher.CalculateHash(in childArena);
        var myMove = moves[0]; 

        ref var childNode = ref _nodeMemoryPool.Get(childIndex);
        
        // Node 3.0: Meno parametri, più velocità
        childNode.PlacementNew(parentIndex, myMove, hash);

        // Linking
        if (lastChildIndex == -1)
        {
            ref var parentNode = ref _nodeMemoryPool.Get(parentIndex);
            parentNode.FirstChildIndex = childIndex;
        }
        else
        {
            ref var prevSibling = ref _nodeMemoryPool.Get(lastChildIndex);
            prevSibling.NextSiblingIndex = childIndex;
        }

        lastChildIndex = childIndex;
    }

    // --- EVALUATION & BACKPROPAGATION ---

    private void Evaluate(int nodeIndex, float[] rewardsBuffer)
    {
        var heuristics = _slotPool.GetHeuristics(nodeIndex);
        var arena = _slotPool.GetArena(nodeIndex);

        Array.Clear(rewardsBuffer);

        // 1. Outcome
        for (var i = 0; i < arena.System.Count; i++)
        {
            var outcome = heuristics.Outcome(i);
            if (outcome != 0.0f) rewardsBuffer[i] = outcome;
        }

        // 2. Heuristics
        Span<float> rawScores = stackalloc float[arena.System.Count];
        heuristics.EvaluateAll(rawScores, true); // Phase Complete = True (Fine turno simulato)

        for (var i = 0; i < arena.System.Count; i++)
        {
            if (rewardsBuffer[i] != 0.0f) continue;
            
            if (arena.System[i].IsDead)
            {
                rewardsBuffer[i] = -1.0f;
                continue;
            }

            rewardsBuffer[i] = MathF.Tanh(rawScores[i] / 150.0f);
        }
    }

    private unsafe void Backpropagate(int startNodeIndex, int leafIndexFromSelect, float[] rewards)
    {
        // 1. Convert to Fixed-Point for Atomic Updates
        for (int i = 0; i < Constants.MaxSnakesCount; i++)
        {
            _atomicRewardsBuffer[i] = (int)(rewards[i] * FIXED_POINT_FACTOR);
        }
        var fixedPointRewards = new ReadOnlySpan<int>(_atomicRewardsBuffer);

        var currentIndex = startNodeIndex;
        
        // Risalita
        while (currentIndex != -1) 
        {
            ref var currentNode = ref _nodeMemoryPool.Get(currentIndex);
            
            // A. Update Stats (Atomico)
            currentNode.UpdateStatsAtomic(fixedPointRewards);
            
            // B. Remove Virtual Loss (Atomico)
            // Decrementiamo SOLO se il nodo faceva parte del percorso di Select.
            // Il percorso di Select va dalla Radice alla Foglia (leafIndex).
            // Se abbiamo espanso un nuovo figlio (startNodeIndex != leafIndex), quel figlio NON ha VL.
            // La Radice NON ha VL (Select non lo incrementa sulla root).
            
            var isRoot = currentNode.ParentIndex == -1;
            var isNewChild = currentIndex != leafIndexFromSelect;

            if (!isRoot && !isNewChild)
            {
                Interlocked.Add(ref currentNode.VirtualLoss, -VIRTUAL_LOSS);
            }
            
            // C. Solver Propagation
            if (currentNode.ParentIndex != -1) 
            {
                PropagateSolverFlags(currentIndex, currentNode.ParentIndex);
            }
            
            currentIndex = currentNode.ParentIndex;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void PropagateSolverFlags(int childIndex, int parentIndex)
    {
        ref var childNode = ref _nodeMemoryPool.Get(childIndex);
        
        if (!childNode.IsSolvedLoss) return;
        
        ref var parentNode = ref _nodeMemoryPool.Get(parentIndex);
            
        if (parentNode.IsSolvedLoss || parentNode.IsSolvedWin) return;

        // Se TUTTI i figli sono SolvedLoss, allora il padre è SolvedLoss.
        var allChildrenLost = true;
        var currentSibling = parentNode.FirstChildIndex;
        while (currentSibling != -1)
        {
            ref var siblingNode = ref _nodeMemoryPool.Get(currentSibling);
            if (!siblingNode.IsSolvedLoss)
            {
                allChildrenLost = false;
                break;
            }

            currentSibling = siblingNode.NextSiblingIndex;
        }

        if (allChildrenLost) 
        {
            parentNode.MarkSolvedLoss();
        }
    }

    public void Reset(RulesetSettings settings)
    {
        _settings = settings;
    }
}