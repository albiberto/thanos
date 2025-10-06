using Thanos.Common;
using Thanos.Memory;
using Thanos.SourceGen;
using Thanos.War;

namespace Thanos.MCST;

public sealed class Worker(SlotMemoryPool slotPool, NodeMemoryPool nodePool)
{
    private const double EXPLORATION_PARAMETER = 1.41;

    private int _nextId = 1;
    private RulesetSettings _settings;
    
    private readonly NodeMemoryPool _nodePool = nodePool;
    private readonly SlotMemoryPool _slotPool = slotPool;

    public void RunIteration(int rootIndex)
    {
        var leafIndex = Select(rootIndex);
        ref var leafNode = ref _nodePool[leafIndex];

        if (leafNode is { IsLeafNode: true, IsTerminal: false })
        {
            Expand(leafIndex);
        }

        var outcome = Evaluate(leafIndex);

        Backpropagate(leafIndex, outcome);
    }

    private int Select(int rootIndex)
    {
        var currentIndex = rootIndex;
        while (true)
        {
            ref var currentNode = ref _nodePool[currentIndex];
            if (currentNode.IsLeafNode || currentNode.IsTerminal)
            {
                return currentIndex;
            }

            var candidateIndex = SelectBestChild(ref currentNode);
            if (candidateIndex == -1)
            {
                currentNode.IsTerminal = true;
                return currentIndex;
            }

            currentIndex = candidateIndex;
        }
    }

    private int SelectBestChild(ref Node parentNode)
    {
        var bestScore = double.MinValue;
        var bestChildIndex = -1;
        
        var logParentVisits = Math.Log(parentNode.Visits);

        var childIndex = parentNode.FirstChildIndex;
        while (childIndex != -1)
        {
            ref var childNode = ref _nodePool[childIndex];

            if (childNode.Visits == 0)
            {
                // Un figlio mai visitato ha priorità assoluta.
                return childIndex;
            }

            // L'approccio Negamax nella retropropagazione fa sì che 'Wins' sia sempre dal punto di vista del genitore.
            // Un valore alto significa che la mossa è buona per il giocatore che la sta scegliendo.
            // La formula UCT classica rimane quindi valida.
            var exploitation = childNode.Wins / childNode.Visits;
            var exploration = EXPLORATION_PARAMETER * Math.Sqrt(logParentVisits / childNode.Visits);
            var uctScore = exploitation + exploration;
            
            if (uctScore > bestScore)
            {
                bestScore = uctScore;
                bestChildIndex = childIndex;
            }

            childIndex = childNode.NextSiblingIndex;
        }

        return bestChildIndex;
    }

    /// <summary>
    /// NUOVO: Espande un nodo generando un figlio per ogni mossa legale del giocatore di turno.
    /// </summary>
    private void Expand(int parentIndex)
    {
        ref var parentNode = ref _nodePool[parentIndex];
        var parentArena = _slotPool.GetArena(parentIndex);

        var playerIndex = parentArena.PlayerToMoveIndex;
        var playerSnake = parentArena.System[playerIndex];

        if (playerSnake.IsDead)
        {
            parentNode.IsTerminal = true;
            return;
        }
        
        // Calcola le mosse strategiche, evitando collisioni testa a testa perse in partenza.
        var safeMoves = GetAdvancedLegalMoves(in parentArena, playerIndex);
        var lastChildIndex = -1;

        foreach (var move in safeMoves)
        {
            var childIndex = ++_nextId;
            var childArena = _slotPool.GetArena(childIndex);
            childArena.CloneFrom(in parentArena);

            // Applica la mossa del singolo giocatore e aggiorna lo stato.
            ApplySingleMove(ref childArena, playerIndex, move);

            // Passa il turno al prossimo serpente vivo.
            childArena.PlayerToMoveIndex = GetNextPlayerIndex(in childArena, playerIndex);

            var hash = ZobristHasher.CalculateHash(in childArena);
            ref var childNode = ref _nodePool[childIndex];
            childNode.PlacementNew(parentIndex, move, hash, parentNode.Generation);

            // Collega il nuovo nodo all'albero.
            if (lastChildIndex == -1)
            {
                parentNode.FirstChildIndex = childIndex;
            }
            else
            {
                _nodePool[lastChildIndex].NextSiblingIndex = childIndex;
            }
            lastChildIndex = childIndex;
        }

        if (lastChildIndex == -1)
        {
            // Nessuna mossa sicura trovata, questo percorso è terminale.
            parentNode.IsTerminal = true;
        }
    }
    
    /// <summary>
    /// NUOVO: Calcola le mosse legali escludendo quelle che portano a una collisione
    /// testa a testa persa o in pareggio.
    /// </summary>
    private List<byte> GetAdvancedLegalMoves(in Arena arena, int snakeIndex)
    {
        var safeMoves = new List<byte>();
        var mySnake = arena.System[snakeIndex];
        if (mySnake.IsDead) return safeMoves;

        // 1. Ottieni le mosse base (contro muri e corpi)
        var potentialMoves = arena.GetLegalMoves(mySnake.Head);
        
        foreach (var move in new[] { Moves.Up, Moves.Down, Moves.Left, Moves.Right })
        {
            if ((potentialMoves & move) == 0) continue;

            var myNextHead = arena.GetNewHeadPosition(mySnake.Head, move);
            var isSquareSafe = true;

            // 2. Controlla se un nemico più forte può contestare la casella
            for (var i = 0; i < arena.System.Count; i++)
            {
                if (i == snakeIndex) continue;

                var enemySnake = arena.System[i];
                // Ignora nemici morti o più corti di noi (vinceremmo la collisione)
                if (enemySnake.IsDead || enemySnake.Length < mySnake.Length) continue;

                // Un nemico è una minaccia se può raggiungere la nostra stessa casella.
                // La distanza di Manhattan è un'ottima e veloce euristica per questo.
                var distance = arena.ManhattanDistance(enemySnake.Head, myNextHead);
                if (distance == 1)
                {
                    isSquareSafe = false;
                    break; // La casella è persa, inutile controllare altri nemici.
                }
            }

            if (isSquareSafe)
            {
                safeMoves.Add(move);
            }
        }

        return safeMoves;
    }

    /// <summary>
    /// NUOVO: Trova l'indice del prossimo serpente vivo a cui passare il turno.
    /// </summary>
    private static int GetNextPlayerIndex(in Arena arena, int currentPlayerIndex)
    {
        var nextIndex = currentPlayerIndex;
        do
        {
            nextIndex = (nextIndex + 1) % arena.System.Count;
        }
        // Continua a ciclare finché non trovi un serpente vivo o hai fatto un giro completo.
        while (arena.System[nextIndex].IsDead && nextIndex != currentPlayerIndex);

        return nextIndex;
    }

    /// <summary>
    /// NUOVO: Applica la mossa di un singolo serpente e aggiorna lo stato dell'arena.
    /// </summary>
    private void ApplySingleMove(ref Arena arena, int snakeIndex, byte move)
    {
        var snake = arena.System[snakeIndex];
        var newHead = arena.GetNewHeadPosition(snake.Head, move);
        
        // Simula la mossa e le sue conseguenze (cibo, danno, etc.)
        // NOTA: Questa logica assume che non ci siano collisioni, dato che sono già state filtrate.
        var hasEaten = arena.Food.IsSet(newHead);
        var damage = arena.Hazards.IsSet(newHead) ? 10 : 1; // Esempio di danno
        var newTail = arena.CalculateNewTailPosition(snake, hasEaten);
        
        arena.Snakes.Xor(snake.Body);
        snake.UpdateAfterMove(newHead, newTail, hasEaten, damage);
        arena.Snakes.Or(snake.Body);
        
        if (hasEaten) arena.Food.Unset(newHead);
        
        // Simula lo spawn del cibo dopo che la mossa è stata completata
        arena.SimulateRandomFoodSpawn(_settings.FoodSpawnChance, _settings.MinimumFood);
    }
    
    private float Evaluate(int leafIndex)
    {
        var heuristics = _slotPool.GetHeuristics(leafIndex);
        // L'outcome e la valutazione sono sempre dal punto di vista del nostro serpente (ID 0).
        var outcome = heuristics.Outcome();
        return outcome != 0.0f ? outcome : heuristics.Evaluate();
    }
    
    /// <summary>
    /// MODIFICATO: Retropropaga il punteggio usando la logica Negamax.
    /// Il punteggio viene invertito ad ogni passo verso l'alto nell'albero.
    /// </summary>
    private void Backpropagate(int startNodeIndex, float rawScore)
    {
        const float scalingFactor = 100.0f;
        var normalizedResult = MathF.Tanh(rawScore / scalingFactor);
        
        var scoreToPropagate = normalizedResult;
        var currentIndex = startNodeIndex;

        while (currentIndex != -1)
        {
            ref var currentNode = ref _nodePool[currentIndex];
            currentNode.UpdateStats(scoreToPropagate);
            
            // Inverti il punteggio per il livello superiore. Una vittoria per il figlio
            // è una sconfitta per il genitore (che è un avversario).
            scoreToPropagate *= -1;
            
            currentIndex = currentNode.ParentIndex;
        }
    }
    
    // Metodi di supporto esistenti
    public int GetMaxId(int rootIndex)
    {
        if (rootIndex == 0) return 0;
        var maxId = rootIndex;
        var queue = new Queue<int>();
        queue.Enqueue(rootIndex);
        while (queue.Count > 0)
        {
            var currentIndex = queue.Dequeue();
            maxId = Math.Max(maxId, currentIndex);
            ref var currentNode = ref _nodePool[currentIndex];
            var childIndex = currentNode.FirstChildIndex;
            while (childIndex != -1)
            {
                queue.Enqueue(childIndex);
                childIndex = _nodePool[childIndex].NextSiblingIndex;
            }
        }
        return maxId;
    }

    public void Reset(int startId) => _nextId = startId;
    public void Reset(int startId, RulesetSettings settings)
    {
        _nextId = startId;
        _settings = settings;
    }
}