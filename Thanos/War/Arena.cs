using System.Runtime.CompilerServices;
using Thanos.Common;
using Thanos.SourceGen;

namespace Thanos.War;

/// <summary>
///     Rappresenta una singola istanza di gioco. È il cervello che orchestra la logica.
/// </summary>
public readonly ref struct Arena(SnakesSystem system, Bitboard food, Bitboard hazards, Bitboard snakes, NeighborsGrid neighborsGrid, Dictionary<string, int> map)
{
    public readonly SnakesSystem System = system;
    
    public readonly Bitboard Food = food;
    public readonly Bitboard Hazards = hazards;
    public readonly Bitboard Snakes = snakes;
    
    private readonly NeighborsGrid _neighborsGrid = neighborsGrid;

    /// <summary>
    ///     Inizializza lo stato dell'arena (usando "Placement New")
    ///     basandosi su una richiesta di configurazione di gioco.
    /// </summary>
    public void InitializeFromRequest(in Request request)
    {
        Food.Clear();
        Hazards.Clear();
        Snakes.Clear();

        for (var i = 0; i < System.Count; i++) System[i].Kill();

        var board = request.Board;

        // --- FASE 2: Inizializzazione di TUTTI i Serpenti in un Unico Ciclo ---
        foreach (var snakeData in board.Snakes)
        {
            // Se il serpente non è mappato, lo saltiamo.
            var snakeIndex = map[snakeData.Id];

            // Prendiamo l'istanza del serpente dal nostro sistema
            var snake = System[snakeIndex];

            // Lo inizializziamo con i dati aggiornati dal server
            snake.Initialize(snakeData.Health, snakeData.Body);

            // Aggiorniamo la griglia generale con la sua posizione
            Snakes.Or(snake.Body);
        }

        // --- FASE 3: Posizionamento di Cibo e Ostacoli ---
        foreach (var foodPosition in board.Food) Food.Set(foodPosition);
        foreach (var hazardPosition in board.Hazards) Hazards.Set(hazardPosition);
    }

    /// <summary>
    ///     Clona lo stato di un'altra Arena in questa istanza.
    ///     Operazione estremamente veloce basata su una copia di memoria.
    /// </summary>
    public void CloneFrom(in Arena source) => source.System.Raw.CopyTo(System.Raw);

    private WarSnake Me => System.Me;

    public byte GetLegalMoves() => GetLegalMoves(Me.Head);

    public byte GetLegalMoves(ushort headPosition)
    {
        byte legalMoves = 0;

        // I controlli ora usano la Grid in modo coerente
        var upPos = _neighborsGrid.Get(headPosition, Moves.Up);
        if (NeighborsGrid.IsValid(upPos) && !Snakes.IsSet(upPos)) legalMoves |= Moves.Up;

        var downPos = _neighborsGrid.Get(headPosition, Moves.Down);
        if (NeighborsGrid.IsValid(downPos) && !Snakes.IsSet(downPos)) legalMoves |= Moves.Down;

        var leftPos = _neighborsGrid.Get(headPosition, Moves.Left);
        if (NeighborsGrid.IsValid(leftPos) && !Snakes.IsSet(leftPos)) legalMoves |= Moves.Left;

        var rightPos = _neighborsGrid.Get(headPosition, Moves.Right);
        if (NeighborsGrid.IsValid(rightPos) && !Snakes.IsSet(rightPos)) legalMoves |= Moves.Right;

        return legalMoves;
    }

    public void ApplySingleMove(int snakeIndex, byte move, Dictionary<int, ushort> newHeads, Dictionary<int, byte> combinedMoves)
    {
        var snake = System[snakeIndex];
        if (snake.IsDead) return;

        // --- FASE 1: Aggiornamento della Testa e Rilevamento di Collisioni Iniziali ---
        var newHead = _neighborsGrid.Get(snake.Head, move);
        newHeads[snakeIndex] = newHead;

        // La logica di "uccisione immediata" ora viene gestita nel Worker.
        // L'Arena si concentra sulla logica di gioco pura.

        var hasEaten = Food.IsSet(newHead);

        var damage = Hazards.IsSet(newHead) ? 10 : 1;
        var newTail = CalculateNewTailPosition(snake, hasEaten);

        // --- FASE 2: Aggiornamento dello Stato Interno del Serpente ---
        // Questi aggiornamenti non influenzano la griglia generale fino alla fine del tick.
        snake.UpdateAfterMove(newHead, newTail, hasEaten, damage);
    
        // Rimuoviamo il serpente dalla griglia principale in preparazione per il riposizionamento.
        Snakes.Xor(snake.Body); 
    }

    public ushort CalculateNewTailPosition(WarSnake snake, bool ateFood)
    {
        // Se il serpente mangia, la coda non si muove.
        if (ateFood) return snake.Tail;

        // Se il serpente è molto corto, la nuova coda è la testa.
        if (snake.Length <= 2) return snake.Head;

        var oldTail = snake.Tail;

        // Controlla i 4 vicini della vecchia coda. Solo uno farà parte
        // del corpo del serpente (il "penultimo" segmento). Quello è la nostra nuova coda.
        var up = _neighborsGrid.Get(oldTail, Moves.Up);
        if (NeighborsGrid.IsValid(up) && snake.IsOnBody(up)) return up;

        var down = _neighborsGrid.Get(oldTail, Moves.Down);
        if (NeighborsGrid.IsValid(down) && snake.IsOnBody(down)) return down;

        var left = _neighborsGrid.Get(oldTail, Moves.Left);
        if (NeighborsGrid.IsValid(left) && snake.IsOnBody(left)) return left;

        var right = _neighborsGrid.Get(oldTail, Moves.Right);
        if (NeighborsGrid.IsValid(right) && snake.IsOnBody(right)) return right;

        // Fallback: non dovrebbe mai succedere in un gioco normale,
        // ma per sicurezza restituiamo la vecchia coda.
        return oldTail;
    }
    
    public ushort GetNewHeadPosition(ushort head, byte move)
{
    return _neighborsGrid.Get(head, move);
}

public bool IsValidPosition(ushort pos)
{
    return NeighborsGrid.IsValid(pos);
}
    
    /// <summary>
    /// Simula lo spawn casuale del cibo usando l'istanza statica e thread-safe Random.Shared.
    /// </summary>
    public void SimulateRandomFoodSpawn(int foodSpawnChance, int minimumFood)
    {
        // 1. Conta il cibo attuale
        var currentFoodCount = Food.PopCount();

        // 2. Soddisfa la regola del "minimumFood"
        var foodToSpawn = minimumFood - currentFoodCount;
        for (var i = 0; i < foodToSpawn; i++)
        {
            var spawnLocation = GetRandomEmptySquare();
            if (NeighborsGrid.IsValid(spawnLocation))
            {
                Food.Set(spawnLocation);
            }
        }

        // 3. Tenta la fortuna con "foodSpawnChance"
        // NOTA: La chiamata ora usa Random.Shared
        if (Random.Shared.Next(0, 100) < foodSpawnChance)
        {
            var spawnLocation = GetRandomEmptySquare();
            if (NeighborsGrid.IsValid(spawnLocation))
            {
                Food.Set(spawnLocation);
            }
        }
    }

    /// <summary>
    /// Trova una coordinata 1D casuale sulla mappa che non sia occupata da un serpente.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ushort GetRandomEmptySquare()
    {
        for (var i = 0; i < 20; i++)
        {
            // NOTA: La chiamata ora usa Random.Shared
            var potentialSpot = (ushort)Random.Shared.Next(0, _neighborsGrid.Area);
            
            if (Snakes.IsUnset(potentialSpot))
            {
                return potentialSpot;
            }
        }
        
        return ushort.MaxValue;
    }
}