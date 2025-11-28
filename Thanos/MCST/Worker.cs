using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Thanos.Common;
using Thanos.Memory;
using Thanos.PreWarm;
using Thanos.SourceGen;
using Thanos.War;

namespace Thanos.MCST;

public sealed class Worker(SlotMemoryPool slotPool, NodeMemoryPool nodePool)
{
    private const double EXPLORATION_PARAMETER = 1.41;
    private const int CHANCE_NODE_VISIT_THRESHOLD = 50; // Progressive Widening

    private int _nextId = 1;
    private RulesetSettings _settings;

    private readonly NodeMemoryPool _nodePool = nodePool;
    private readonly SlotMemoryPool _slotPool = slotPool;

    private static readonly byte[] AllMoves = [Moves.Up, Moves.Down, Moves.Left, Moves.Right];

    // Buffer riutilizzabile per la backpropagation (evita allocazioni)
    private readonly float[] _rewardsBuffer = new float[Constants.MaxSnakesCount];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RunIteration(int area, int rootIndex)
    {
        // 1. SELECTION
        var leafIndex = Select(rootIndex);
        ref var leafNode = ref _nodePool[leafIndex];

        // 2. EXPANSION (se non terminale e non già risolto)
        if (leafNode.IsLeafNode && !leafNode.IsTerminal && !leafNode.IsSolvedWin && !leafNode.IsSolvedLoss)
        {
            Expand(leafIndex, ref leafNode, area);
        }

        // Se dopo l'espansione non è più una foglia, scendiamo in uno dei nuovi figli
        // per valutare quello invece del padre (migliora la precisione immediata)
        var nodeToEvaluate = leafIndex;
        if (!leafNode.IsLeafNode)
        {
            // Selezioniamo il primo figlio o uno a caso per la valutazione iniziale
            nodeToEvaluate = leafNode.FirstChildIndex;
        }

        // 3. EVALUATION
        Evaluate(nodeToEvaluate, _rewardsBuffer);

        // 4. BACKPROPAGATION
        Backpropagate(nodeToEvaluate, _rewardsBuffer);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int Select(int rootIndex)
    {
        var currentIndex = rootIndex;

        while (true)
        {
            ref var currentNode = ref _nodePool[currentIndex];

            // Condizioni di stop: Foglia, Terminale, o Risolto (Win/Loss)
            if (currentNode.IsLeafNode || currentNode.IsTerminal || currentNode.IsSolvedWin || currentNode.IsSolvedLoss)
            {
                return currentIndex;
            }

            // Se è un Chance Node (Turno Ambiente), selezione stocastica
            if (currentNode.IsChanceNode)
            {
                var outcomeIndex = SelectChanceOutcome(ref currentNode);
                if (outcomeIndex == -1) return currentIndex; // Fallback
                currentIndex = outcomeIndex;
                continue;
            }

            // Se è un Player Node, selezione MaxN + UCT
            var bestChild = SelectBestChildMaxN(ref currentNode);
            if (bestChild == -1)
            {
                currentNode.MarkTerminal();
                return currentIndex;
            }

            currentIndex = bestChild;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe int SelectBestChildMaxN(ref Node parentNode)
    {
        var bestScore = double.MinValue;
        var bestChildIndex = -1;
        var logParentVisits = Math.Log(parentNode.Visits);
        
        // Chi deve muovere in questo nodo?
        var playerIndex = parentNode.PlayerIndex;

        var childIndex = parentNode.FirstChildIndex;
        while (childIndex != -1)
        {
            ref var childNode = ref _nodePool[childIndex];

            // SOLVER: Se un figlio è una vittoria certa per il giocatore corrente, prendilo subito!
            // Nota: IsSolvedWin è relativo al giocatore che ha fatto la mossa che ha portato a quello stato.
            // Qui childNode rappresenta lo stato DOPO la mossa di 'playerIndex'.
            // Quindi se childNode.IsSolvedWin, significa che playerIndex ha vinto.
            if (childNode.IsSolvedWin) return childIndex;

            // Se il figlio è una sconfitta certa, evitalo se possibile
            if (childNode.IsSolvedLoss)
            {
                childIndex = childNode.NextSiblingIndex;
                continue;
            }

            if (childNode.Visits == 0) return childIndex; // Priorità nodi inesplorati

            // MaxN Exploitation: Punteggio relativo al giocatore che sta decidendo
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

        return bestChildIndex; // Può ritornare -1 se tutti i figli sono SolvedLoss
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int SelectChanceOutcome(ref Node parentNode)
    {
        // BUG FIX: Non usare SelectBestChildMaxN qui! L'Environment (Player 255) non ha Rewards[255].
        
        var firstChild = parentNode.FirstChildIndex;
        
        // Se non ha figli (non dovrebbe succedere se siamo qui), fallback
        if (firstChild == -1) return -1;
        
        ref var firstNode = ref _nodePool[firstChild];
        var secondChild = firstNode.NextSiblingIndex;

        // Se c'è solo uno scenario (No Spawn), andiamo lì.
        if (secondChild == -1) return firstChild;

        // Se ci sono due scenari (No Spawn vs Spawn), dobbiamo scegliere.
        // Strategia: Campionamento Monte Carlo basato sulle visite per mantenere la proporzione reale?
        // Oppure forzare la distribuzione di probabilità nota (85% vs 15%)?
        
        // Approccio MCTS Puro: UCT sceglie il nodo meno esplorato per bilanciare.
        // Ma qui vogliamo che l'albero rifletta la realtà (No Spawn è molto più frequente).
        
        // Usiamo un semplice Random Weighted Choice per guidare l'esplorazione verso la distribuzione reale.
        // Assumiamo che il Primo Figlio sia "No Spawn" (creato per primo in ExpandChanceNode)
        
        // 15% probabilità di esplorare lo spawn cibo, 85% no.
        // Questo farà sì che il ramo "No Spawn" riceva l'85% delle visite, rendendo le sue statistiche molto solide.
        var pickSpawn = Random.Shared.NextDouble() < _settings.FoodSpawnChance / 100.0;
        
        if (pickSpawn) return secondChild; // Spawn Node
        return firstChild; // No Spawn Node
    }

    private void Expand(int parentIndex, ref Node parentNode, int area)
    {
        if (parentNode.IsChanceNode)
        {
            ExpandChanceNode(parentIndex, ref parentNode, area);
        }
        else
        {
            ExpandPlayerNode(parentIndex, ref parentNode, area);
        }
    }

    private void ExpandPlayerNode(int parentIndex, ref Node parentNode, int area)
    {
        var playerIndex = parentNode.PlayerIndex;
        var arena = _slotPool.GetArena(parentIndex);
        var snake = arena.System[playerIndex];

        if (snake.IsDead)
        {
            parentNode.MarkTerminal();
            parentNode.MarkSolvedLoss();
            return;
        }

        var legalMoves = arena.GetLegalMoves(snake.Head, snake.Tail, snake.ElementBeforeTail);
        
        if (legalMoves == 0)
        {
            parentNode.MarkTerminal();
            parentNode.MarkSolvedLoss();
            return;
        }

        // --- INIZIO PRUNING AVANZATO ---
        // Filtriamo le mosse che portano a morte certa (vicoli ciechi immediati)
        // Usiamo una maschera 'prunedMoves' che contiene solo le mosse che superano il check di sicurezza.
        byte prunedMoves = 0;
        var safeMoveCount = 0;

        foreach (var move in AllMoves)
        {
            if ((legalMoves & move) == 0) continue;
            
            // Verifica rapida: La mossa porta in una trappola immediata?
            if (!IsMoveRisky(in arena, snake.Head, move))
            {
                prunedMoves |= move;
                safeMoveCount++;
            }
        }

        // SAFETY FALLBACK: Se ho potato TUTTE le mosse (es. sono costretto a entrare in un tunnel),
        // allora devo per forza esplorare le mosse originali "rischiose".
        // Meglio rischiare la morte che suicidarsi subito.
        var movesToExpand = (safeMoveCount > 0) ? prunedMoves : legalMoves;
        // --- FINE PRUNING AVANZATO ---

        var nextPlayerIndex = GetNextPlayerIndex(in arena, playerIndex);
        var isNextChance = nextPlayerIndex == Constants.EnvironmentPlayerIndex;
        var actualNextPlayer = isNextChance ? (byte)Constants.EnvironmentPlayerIndex : (byte)nextPlayerIndex;

        var lastChildIndex = -1;
        foreach (var move in AllMoves)
        {
            // Usiamo movesToExpand invece di legalMoves
            if ((movesToExpand & move) == 0) continue;

            var childIndex = ++_nextId;
            var childArena = _slotPool.GetArena(childIndex);
            
            childArena.CloneFrom(in arena);
            
            var snakeToMove = childArena.System[playerIndex];
            ApplySingleMove(in childArena, ref snakeToMove, move, area);

            var hash = ZobristHasher.CalculateHash(in childArena);

            ref var childNode = ref _nodePool[childIndex];
            childNode.PlacementNew(parentIndex, move, hash, actualNextPlayer, isNextChance);

            if (lastChildIndex == -1) parentNode.FirstChildIndex = childIndex;
            else _nodePool[lastChildIndex].NextSiblingIndex = childIndex;

            lastChildIndex = childIndex;
        }
    }

    /// <summary>
    /// Verifica leggera se una mossa porta in una casella con meno di 2 uscite libere (potenziale trappola).
    /// Non è un floodfill completo, è un check locale velocissimo.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsMoveRisky(in Arena arena, ushort currentHead, byte move)
    {
        // Calcoliamo la posizione futura della testa
        var newHead = arena.GetNewHeadPosition(currentHead, move);
        
        // Se non è valida (es. fuori mappa), è rischiosa (ma GetLegalMoves dovrebbe averla già esclusa)
        if (!NeighborsGrid.IsValid(newHead)) return true;

        // Contiamo le uscite libere dalla nuova testa
        var openExits = 0;
        
        // Controlliamo i 4 vicini della nuova testa
        // Nota: Dobbiamo usare NeighborsGrid per ottenere gli indici, ma arena.Snakes per le collisioni
        // NON possiamo usare GetLegalMoves qui perché richiederebbe Tail/PrevTail che cambieranno
        
        // Up
        var n = arena.GetNewHeadPosition(newHead, Moves.Up);
        if (NeighborsGrid.IsValid(n) && !arena.Snakes.IsSet(n)) openExits++;
        
        // Down
        n = arena.GetNewHeadPosition(newHead, Moves.Down);
        if (NeighborsGrid.IsValid(n) && !arena.Snakes.IsSet(n)) openExits++;
        
        // Left
        n = arena.GetNewHeadPosition(newHead, Moves.Left);
        if (NeighborsGrid.IsValid(n) && !arena.Snakes.IsSet(n)) openExits++;
        
        // Right
        n = arena.GetNewHeadPosition(newHead, Moves.Right);
        if (NeighborsGrid.IsValid(n) && !arena.Snakes.IsSet(n)) openExits++;

        // Se dalla nuova posizione ho 0 o 1 via di fuga, è molto probabile che sia una trappola
        // (a meno che non sia l'unica mossa possibile, ma quello lo gestisce il fallback)
        return openExits < 2;
    }

    private void ExpandChanceNode(int parentIndex, ref Node parentNode, int area)
    {
        // 1. Genera SEMPRE lo scenario "Nessun Cibo Spawnato" (Alta probabilità)
        CreateEnvironmentChild(parentIndex, area, spawnFood: false);

        // 2. Progressive Widening: Se questo nodo è molto visitato, genera anche scenari di spawn
        if (parentNode.Visits > CHANCE_NODE_VISIT_THRESHOLD)
        {
            // TODO: Generare cibo in posizioni diverse? Per ora uno spawn casuale generico
             CreateEnvironmentChild(parentIndex, area, spawnFood: true);
        }
    }

    private void CreateEnvironmentChild(int parentIndex, int area, bool spawnFood)
    {
        var childIndex = ++_nextId;
        var parentArena = _slotPool.GetArena(parentIndex);
        var childArena = _slotPool.GetArena(childIndex);
        
        childArena.CloneFrom(in parentArena);

        if (spawnFood)
        {
            childArena.SimulateRandomFoodSpawn(_settings.FoodSpawnChance, _settings.MinimumFood, area);
        }

        var hash = ZobristHasher.CalculateHash(in childArena);
        
        ref var childNode = ref _nodePool[childIndex];
        ref var parentNode = ref _nodePool[parentIndex];
        
        // Dopo l'ambiente, tocca di nuovo al Giocatore 0
        childNode.PlacementNew(parentIndex, Moves.None, hash, 0, false); 

        // Link in coda ai figli esistenti
        if (parentNode.FirstChildIndex == -1)
        {
            parentNode.FirstChildIndex = childIndex;
        }
        else
        {
            var sibling = parentNode.FirstChildIndex;
            while (_nodePool[sibling].NextSiblingIndex != -1) sibling = _nodePool[sibling].NextSiblingIndex;
            _nodePool[sibling].NextSiblingIndex = childIndex;
        }
    }

    private static int GetNextPlayerIndex(in Arena arena, int currentPlayerIndex)
    {
        // Controlliamo se il round è finito (tutti i serpenti vivi hanno mosso)
        // La logica attuale assume un ordine sequenziale 0->1->2->3.
        // Se siamo all'ultimo serpente, il prossimo step è l'Ambiente.
        
        if (currentPlayerIndex >= arena.System.Count - 1)
            return Constants.EnvironmentPlayerIndex;

        // Altrimenti trova il prossimo vivo
        var next = currentPlayerIndex + 1;
        while (next < arena.System.Count && arena.System[next].IsDead)
        {
            next++;
        }

        if (next >= arena.System.Count) return Constants.EnvironmentPlayerIndex;
        return next;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ApplySingleMove(in Arena arena, ref WarSnake snake, byte move, int area)
    {
        var newHead = arena.GetNewHeadPosition(snake.Head, move);
        var hasEaten = arena.Food.IsSet(newHead);
        
        // Danneggiamento da Hazard
        var damage = arena.Hazards.IsSet(newHead) ? _settings.HazardDamagePerTurn : 1; // Default decay 1

        arena.Snakes.Xor(snake.Body);
        snake.UpdateAfterMove(newHead, hasEaten, damage);
        arena.Snakes.Or(snake.Body);

        if (hasEaten) arena.Food.Unset(newHead);
    }

    private void Evaluate(int nodeIndex, float[] rewardsBuffer)
        {
            var heuristics = _slotPool.GetHeuristics(nodeIndex);
            var arena = _slotPool.GetArena(nodeIndex);
    
            // Resetta buffer
            Array.Clear(rewardsBuffer);
    
            // 1. Controllo Vittoria/Sconfitta Definitiva (Outcome)
            // Dobbiamo controllare l'outcome per OGNI giocatore
            // Rimosso 'terminalFound' perché non utilizzato
            
            for(var i=0; i < arena.System.Count; i++)
            {
                var outcome = heuristics.Outcome(i);
                if (outcome != 0.0f)
                {
                    rewardsBuffer[i] = outcome; // 1.0 se vinto, -1.0 se morto
                }
            }
            
            // 2. Euristica Completa (MaxN)
            // Calcoliamo i punteggi euristici per tutti
            Span<float> rawScores = stackalloc float[arena.System.Count];
            heuristics.EvaluateAll(rawScores);
    
            for (var i = 0; i < arena.System.Count; i++)
            {
                // Se abbiamo già un outcome terminale (vittoria/morte), prevale quello
                if (rewardsBuffer[i] != 0.0f) continue;
                
                if (arena.System[i].IsDead)
                {
                    rewardsBuffer[i] = -1.0f;
                    continue;
                }
    
                // Normalizzazione: Tanh per portare i punteggi arbitrari nel range [-1, 1]
                rewardsBuffer[i] = MathF.Tanh(rawScores[i] / 150.0f);
            }
        }

    private unsafe void Backpropagate(int startNodeIndex, float[] rewards)
    {
        var currentIndex = startNodeIndex;

        while (currentIndex != -1)
        {
            ref var currentNode = ref _nodePool[currentIndex];
            
            // Aggiorna statistiche (Atomic updates non strettamente necessari se ThreadLocal)
            currentNode.UpdateStats(rewards);

            // --- SOLVER PROPAGATION ---
            // Propaga i flag Win/Loss verso l'alto
            if (currentNode.ParentIndex != -1)
            {
                PropagateSolverFlags(currentIndex, currentNode.ParentIndex);
            }

            currentIndex = currentNode.ParentIndex;
        }
    }

// Inserisci questo metodo in Worker.cs al posto di quello vecchio vuoto

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void PropagateSolverFlags(int childIndex, int parentIndex)
    {
        ref var childNode = ref _nodePool[childIndex];
        
        // 1. Propagazione SCONFITTA (Dead End Propagation)
        // Se il figlio appena aggiornato è una sconfitta certa, controlliamo se il padre ha altre opzioni.
        if (childNode.IsSolvedLoss)
        {
            ref var parentNode = ref _nodePool[parentIndex];
            
            // Se il padre è già risolto, inutile controllare
            if (parentNode.IsSolvedLoss || parentNode.IsSolvedWin) return;

            // Se il padre è un Chance Node (Ambiente), la logica è diversa:
            // L'ambiente "perde" solo se TUTTI gli scenari sono impossibili (molto raro).
            if (parentNode.IsChanceNode) return;

            // Se è un nodo Giocatore:
            // Controlliamo se TUTTI i figli sono SolvedLoss.
            // Se non c'è nessuna mossa che salva il giocatore, allora il padre è SolvedLoss.
            
            var allChildrenLost = true;
            var currentSibling = parentNode.FirstChildIndex;
            while (currentSibling != -1)
            {
                ref var siblingNode = ref _nodePool[currentSibling];
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
            return;
        }

        // 2. Propagazione VITTORIA (Win Propagation)
        // Se il figlio è una vittoria certa E garantisce la vittoria al giocatore che muove nel padre.
        // (Logica complessa in 4-player, la omettiamo per sicurezza per evitare falsi positivi.
        //  La Dead End Propagation è la parte più critica per la sopravvivenza).
    }

    public void Reset(int startId) => _nextId = startId;
    public void Reset(int startId, RulesetSettings settings)
    {
        _nextId = startId;
        _settings = settings;
    }
}