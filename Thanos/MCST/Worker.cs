using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics; // NECESSARIO PER VECTOR128
using Thanos.Abstract;
using Thanos.Common;
using Thanos.SourceGen;
using Thanos.War;

namespace Thanos.MCST;

public sealed class Worker(ISlotMemoryPool slotPool, INodeMemoryPool nodeMemoryPool)
{
    private const double EXPLORATION_PARAMETER = 1.41;
    private const int CHANCE_NODE_VISIT_THRESHOLD = 50;

    private RulesetSettings _settings;

    private readonly INodeMemoryPool _nodeMemoryPool = nodeMemoryPool;
    private readonly ISlotMemoryPool _slotPool = slotPool;

    private static readonly byte[] AllMoves = [Moves.Up, Moves.Down, Moves.Left, Moves.Right];
    
    // Buffer riutilizzabile per i rewards (allocato una volta sola)
    private readonly float[] _rewardsBuffer = new float[Constants.MaxSnakesCount];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RunIteration(int area, int rootIndex)
    {
        // 1. Selection
        var leafIndex = Select(rootIndex);
        
        // Controllo validità selezione (se pool corrotto o logic error)
        if (leafIndex == -1) return;

        ref var leafNode = ref _nodeMemoryPool.Get(leafIndex);

        // 2. Expansion
        // Espandiamo solo se è una foglia non terminale e non risolta
        if (leafNode.IsLeafNode && leafNode is { IsTerminal: false, IsSolvedWin: false, IsSolvedLoss: false }) 
        {
            Expand(leafIndex, ref leafNode, area);
        }

        // 3. Evaluation
        // Se abbiamo espanso, valutiamo il primo figlio (best guess), altrimenti il nodo stesso
        var nodeToEvaluate = leafIndex;
        if (!leafNode.IsLeafNode) nodeToEvaluate = leafNode.FirstChildIndex;

        Evaluate(nodeToEvaluate, _rewardsBuffer);

        // 4. Backpropagation
        Backpropagate(nodeToEvaluate, _rewardsBuffer);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int Select(int rootIndex)
    {
        var currentIndex = rootIndex;
        
        // Safety loop break (opzionale, per evitare loop infiniti in dev)
        var depth = 0; 
        const int maxDepth = 1000;

        while (depth++ < maxDepth)
        {
            ref var currentNode = ref _nodeMemoryPool.Get(currentIndex);

            if (currentNode.IsLeafNode || currentNode.IsTerminal || currentNode.IsSolvedWin || currentNode.IsSolvedLoss)
                return currentIndex;

            if (currentNode.IsChanceNode)
            {
                var outcomeIndex = SelectChanceOutcome(ref currentNode);
                if (outcomeIndex == -1) return currentIndex; // Nessun outcome disponibile (improbabile se non terminale)
                currentIndex = outcomeIndex;
                continue;
            }

            var bestChild = SelectBestChildMaxN(ref currentNode);
            if (bestChild == -1)
            {
                // Non dovrebbe accadere se IsLeafNode è false, ma per sicurezza:
                currentNode.MarkTerminal();
                return currentIndex;
            }

            currentIndex = bestChild;
        }
        
        return currentIndex;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe int SelectBestChildMaxN(ref Node parentNode)
    {
        var bestScore = double.MinValue;
        var bestChildIndex = -1;
        var logParentVisits = Math.Log(parentNode.Visits + 1); // +1 per evitare Log(0) o problemi numerici
        var playerIndex = parentNode.PlayerIndex;

        var childIndex = parentNode.FirstChildIndex;
        while (childIndex != -1)
        {
            ref var childNode = ref _nodeMemoryPool.Get(childIndex);

            // Solver Logic: se trovo una vittoria certa, la prendo subito
            if (childNode.IsSolvedWin) return childIndex;
            
            // Solver Logic: se è una sconfitta certa, la evito a meno che non sia l'unica mossa
            if (childNode.IsSolvedLoss)
            {
                childIndex = childNode.NextSiblingIndex;
                continue;
            }

            // FPU (First Play Urgency): se non visitato, visitiamo subito per avere una stima
            if (childNode.Visits == 0) return childIndex;

            var exploitation = childNode.Rewards[playerIndex] / childNode.Visits;
            var exploration = EXPLORATION_PARAMETER * Math.Sqrt(logParentVisits / childNode.Visits);
            var uctScore = exploitation + exploration;

            if (uctScore > bestScore)
            {
                bestScore = uctScore;
                bestChildIndex = childIndex;
            }

            childIndex = childNode.NextSiblingIndex;
        }

        // Se tutti i figli sono SolvedLoss, siamo costretti a prenderne uno (il primo disponibile o il meno peggio)
        // In questo caso il Select loop marcherà il nodo corrente come SolvedLoss in backprop.
        if (bestChildIndex == -1 && parentNode.FirstChildIndex != -1)
        {
             return parentNode.FirstChildIndex;
        }

        return bestChildIndex;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int SelectChanceOutcome(ref Node parentNode)
    {
        var firstChild = parentNode.FirstChildIndex;
        if (firstChild == -1) return -1;

        ref var firstNode = ref _nodeMemoryPool.Get(firstChild);
        var secondChild = firstNode.NextSiblingIndex;

        // Se c'è un solo figlio (es. no food spawn), torniamo quello
        if (secondChild == -1) return firstChild;

        // Simuliamo la probabilità di spawn del cibo
        var pickSpawn = Random.Shared.NextDouble() < _settings.FoodSpawnChance / 100.0;
        return pickSpawn ? secondChild : firstChild;
    }

    private void Expand(int parentIndex, ref Node parentNode, int area)
    {
        // Se i pool sono pieni, non possiamo espandere.
        // Controlliamo preventivamente la capacità residua? No, proviamo ad allocare.
        
        if (parentNode.IsChanceNode) ExpandChanceNode(parentIndex, ref parentNode, area);
        else ExpandPlayerNode(parentIndex, ref parentNode, area);
    }

    private void ExpandPlayerNode(int parentIndex, ref Node parentNode, int area)
    {
        var playerIndex = parentNode.PlayerIndex;
        var arena = _slotPool.GetArena(parentIndex);
        var snake = arena.System[playerIndex];

        if (snake.IsDead)
        {
            parentNode.MarkTerminal();
            return;
        }

        var legalMoves = arena.GetLegalMoves(snake.Head, snake.Tail, snake.ElementBeforeTail, playerIndex);

        if (legalMoves == 0)
        {
            parentNode.MarkTerminal();
            return;
        }

        // --- Move Pruning (Opzionale) ---
        byte prunedMoves = 0;
        var safeMoveCount = 0;
        foreach (var move in AllMoves)
        {
            if ((legalMoves & move) == 0) continue;
            if (!IsMoveRisky(in arena, snake.Head, move))
            {
                prunedMoves |= move;
                safeMoveCount++;
            }
        }
        var movesToExpand = safeMoveCount > 0 ? prunedMoves : legalMoves;
        // --------------------------------

        var nextPlayerIndex = GetNextPlayerIndex(in arena, playerIndex);
        var isNextChance = nextPlayerIndex == Constants.EnvironmentPlayerIndex;
        var actualNextPlayer = isNextChance ? (byte)Constants.EnvironmentPlayerIndex : (byte)nextPlayerIndex;

        var lastChildIndex = -1;

        foreach (var move in AllMoves)
        {
            if ((movesToExpand & move) == 0) continue;

            // --- ALLOCAZIONE SINCRONIZZATA ---
            var childIndex = _nodeMemoryPool.Allocate();
            var childSlotIndex = _slotPool.Allocate();

            // Gestione Out of Memory (Pool Pieni)
            if (childIndex == -1 || childSlotIndex == -1)
            {
                // Rollback parziale se uno dei due è fallito (raro ma possibile se capacity diverse)
                // Per semplicità, in una competizione, se finisce la memoria ci fermiamo.
                return; 
            }

            // Inizializza lo stato del figlio
            var childArena = _slotPool.GetArena(childIndex);
            childArena.CloneFrom(in arena);

            var snakeToMove = childArena.System[playerIndex];
            ApplySingleMove(in childArena, ref snakeToMove, move, area);

            var hash = ZobristHasher.CalculateHash(in childArena);

            ref var childNode = ref _nodeMemoryPool.Get(childIndex);
            childNode.PlacementNew(parentIndex, move, hash, actualNextPlayer, isNextChance);

            // Linking della lista concatenata dei fratelli
            if (lastChildIndex == -1) 
            {
                parentNode.FirstChildIndex = childIndex;
            }
            else 
            {
                _nodeMemoryPool.Get(lastChildIndex).NextSiblingIndex = childIndex;
            }

            lastChildIndex = childIndex;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsMoveRisky(in Arena arena, ushort currentHead, byte move) => false;

    private void ExpandChanceNode(int parentIndex, ref Node parentNode, int area)
    {
        // Genera sempre il caso "No Food Spawn"
        var success1 = CreateEnvironmentChild(parentIndex, area, false);
        
        // Se il nodo è molto visitato, espandiamo anche il caso "Food Spawn"
        if (success1 && parentNode.Visits > CHANCE_NODE_VISIT_THRESHOLD)
        {
            CreateEnvironmentChild(parentIndex, area, true);
        }
    }

    private bool CreateEnvironmentChild(int parentIndex, int area, bool spawnFood)
    {
        // --- ALLOCAZIONE SINCRONIZZATA ---
        var childIndex = _nodeMemoryPool.Allocate();
        var childSlotIndex = _slotPool.Allocate();

        if (childIndex == -1 || childSlotIndex == -1) return false;

        var parentArena = _slotPool.GetArena(parentIndex);
        var childArena = _slotPool.GetArena(childIndex);
        
        childArena.CloneFrom(in parentArena);

        if (spawnFood) 
        {
            childArena.SimulateRandomFoodSpawn(_settings.FoodSpawnChance, _settings.MinimumFood, area);
        }

        var hash = ZobristHasher.CalculateHash(in childArena);

        ref var childNode = ref _nodeMemoryPool.Get(childIndex);
        ref var parentNode = ref _nodeMemoryPool.Get(parentIndex);

        var firstAlive = GetFirstAlivePlayerIndex(in childArena);
        var isNextChance = firstAlive == Constants.EnvironmentPlayerIndex; // Tutti morti?
        var nextPlayer = (byte)firstAlive;

        childNode.PlacementNew(parentIndex, Moves.None, hash, nextPlayer, isNextChance);
        
        if (isNextChance) childNode.MarkTerminal();

        // Linking
        if (parentNode.FirstChildIndex == -1)
        {
            parentNode.FirstChildIndex = childIndex;
        }
        else
        {
            var sibling = parentNode.FirstChildIndex;
            while (_nodeMemoryPool.Get(sibling).NextSiblingIndex != -1) 
            {
                sibling = _nodeMemoryPool.Get(sibling).NextSiblingIndex;
            }
            _nodeMemoryPool.Get(sibling).NextSiblingIndex = childIndex;
        }

        return true;
    }

    private static int GetNextPlayerIndex(in Arena arena, int currentPlayerIndex)
    {
        var next = currentPlayerIndex + 1;
        while (next < arena.System.Count && arena.System[next].IsDead) next++;
        if (next >= arena.System.Count) return Constants.EnvironmentPlayerIndex;
        return next;
    }

    private static int GetFirstAlivePlayerIndex(in Arena arena)
    {
        var next = 0;
        while (next < arena.System.Count && arena.System[next].IsDead) next++;
        if (next >= arena.System.Count) return Constants.EnvironmentPlayerIndex;
        return next;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ApplySingleMove(in Arena arena, ref WarSnake snake, byte move, int area)
    {
        var newHead = arena.GetNewHeadPosition(snake.Head, move);

        arena.Snakes.Xor(snake.Body);

        if (arena.Snakes.IsSet(newHead))
            for (var i = 0; i < arena.System.Count; i++)
            {
                var enemy = arena.System[i];
                if (enemy.IsOnBody(newHead))
                {
                    if (enemy.Head == newHead)
                    {
                        if (snake.Length <= enemy.Length) snake.Kill();
                        if (snake.Length >= enemy.Length)
                        {
                            arena.System[i].Kill();
                            arena.Snakes.Xor(arena.System[i].Body);
                        }
                    }
                    else
                    {
                        snake.Kill();
                    }
                }
            }

        if (snake.IsDead) return;

        var hasEaten = arena.Food.IsSet(newHead);
        var damage = arena.Hazards.IsSet(newHead) ? _settings.HazardDamagePerTurn : 1;

        snake.UpdateAfterMove(newHead, hasEaten, damage);
        arena.Snakes.Or(snake.Body);

        if (hasEaten) arena.Food.Unset(newHead);
    }

    private void Evaluate(int nodeIndex, float[] rewardsBuffer)
    {
        var heuristics = _slotPool.GetHeuristics(nodeIndex);
        var arena = _slotPool.GetArena(nodeIndex);

        ref var node = ref _nodeMemoryPool.Get(nodeIndex);
        
        // Determina se il turno è completo (Environment o Player 0 a muovere)
        var isPhaseComplete = node.PlayerIndex is Constants.EnvironmentPlayerIndex or 0;

        Array.Clear(rewardsBuffer);

        // 1. Terminal/Outcome check
        for (var i = 0; i < arena.System.Count; i++)
        {
            var outcome = heuristics.Outcome(i);
            if (outcome != 0.0f) rewardsBuffer[i] = outcome;
        }

        // 2. Heuristic evaluation
        // Usiamo stackalloc per passare lo Span richiesto da EvaluateAll
        Span<float> rawScores = stackalloc float[arena.System.Count];
        heuristics.EvaluateAll(rawScores, isPhaseComplete);

        for (var i = 0; i < arena.System.Count; i++)
        {
            // Se abbiamo già un outcome definitivo (es. vittoria/morte), usiamolo
            if (rewardsBuffer[i] != 0.0f) continue;
            
            if (arena.System[i].IsDead)
            {
                rewardsBuffer[i] = -1.0f;
                continue;
            }

            // Normalizzazione score euristico in [-1, 1] tramite Tanh
            rewardsBuffer[i] = MathF.Tanh(rawScores[i] / 150.0f);
        }
    }

    private unsafe void Backpropagate(int startNodeIndex, float[] rewards)
    {
        // FIX: Caricamento SIMD una volta sola per tutta la risalita.
        // Questo è molto più veloce che chiamare un overload scalare per ogni nodo o creare il vettore dentro il loop.
        var rewardsVector = Vector128.Create(rewards);

        var currentIndex = startNodeIndex;
        while (currentIndex != -1) // Risaliamo fino alla root (Parent == -1)
        {
            ref var currentNode = ref _nodeMemoryPool.Get(currentIndex);
            
            // Passiamo il vettore hardware direttamente
            currentNode.UpdateStats(rewardsVector);
            
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
        
        // Se un figlio è una sconfitta certa, controlliamo se TUTTI i fratelli sono sconfitte.
        if (!childNode.IsSolvedLoss) return;
        
        ref var parentNode = ref _nodeMemoryPool.Get(parentIndex);
            
        if (parentNode.IsSolvedLoss || parentNode.IsSolvedWin) return;
        if (parentNode.IsChanceNode) return; // Chance node ha logiche diverse (media pesata)

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
