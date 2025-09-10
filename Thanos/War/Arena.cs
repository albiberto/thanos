using Thanos.Common;
using Thanos.SourceGen;

namespace Thanos.War;

/// <summary>
///     Rappresenta una singola istanza di gioco. È il cervello che orchestra la logica.
/// </summary>
public readonly ref struct Arena(SnakesSystem system, Bitboard food, Bitboard hazards, Bitboard snakes, NeighborsGrid neighborsGrid, Dictionary<Guid, int> map)
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

    public void ApplySingleMove(byte move)
    {
        var me = Me;
        if (me.IsDead) return;

        var head = me.Head;
        var oldTail = me.Tail;

        // --- 1. L'ARENA PRENDE LE DECISIONI ---
        var newHead = _neighborsGrid.Get(head, move);

        if (!NeighborsGrid.IsValid(newHead))
        {
            me.Kill();
            Snakes.Xor(me.Body);
            return;
        }

        var hasEaten = Food.IsSet(newHead);

        if (Snakes.IsSet(newHead))
        {
            var isMovingOntoOwnVacatingTail = newHead == oldTail && !hasEaten;
            if (!isMovingOntoOwnVacatingTail)
            {
                me.Kill();
                Snakes.Xor(me.Body);
                return;
            }
        }

        var damage = Hazards.IsSet(newHead) ? 10 : 1;
        var newTail = CalculateNewTailPosition(me, hasEaten); // Logica di gioco da implementare

        // --- 2. L'ARENA COMANDA AL CORPO (WARSNAKE) DI AGGIORNARSI ---
        me.UpdateAfterMove(newHead, newTail, hasEaten, damage);

        // --- 3. L'ARENA COMANDA AL MONDO (GRID) DI AGGIORNARSI ---
        Snakes.Set(newHead);

        switch (hasEaten)
        {
            case false:
                Snakes.Unset(oldTail);
                break;
            case true:
                Food.Unset(newHead);
                break;
        }
    }

    private ushort CalculateNewTailPosition(WarSnake snake, bool ateFood)
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
}