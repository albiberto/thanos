using Thanos.Common;
using Thanos.SourceGen;

namespace Thanos.War;

/// <summary>
///     Rappresenta una singola istanza di gioco. È il cervello che orchestra la logica.
/// </summary>
public readonly ref struct Arena(SnakesSystem system, Grid grid, Dictionary<Guid, int> map)
{
    public readonly SnakesSystem System = system;
    public Grid Grid { get; } = grid;

    /// <summary>
    ///     Inizializza lo stato dell'arena (usando "Placement New")
    ///     basandosi su una richiesta di configurazione di gioco.
    /// </summary>
    public void InitializeFromRequest(in Request request)
    {
        Grid.Food.Clear();
        Grid.Hazards.Clear();
        Grid.Snakes.Clear(); 

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
            Grid.Snakes.Or(snake.Body);
        }

        // --- FASE 3: Posizionamento di Cibo e Ostacoli ---
        foreach (var foodPosition in board.Food) Grid.Food.Set(foodPosition);
        foreach (var hazardPosition in board.Hazards) Grid.Hazards.Set(hazardPosition);
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
        var upPos = Grid.GetNeighbor(headPosition, Moves.Up);
        if (Grid.IsValid(upPos) && !Grid.IsOccupied(upPos)) legalMoves |= Moves.Up;

        var downPos = Grid.GetNeighbor(headPosition, Moves.Down);
        if (Grid.IsValid(downPos) && !Grid.IsOccupied(downPos)) legalMoves |= Moves.Down;

        var leftPos = Grid.GetNeighbor(headPosition, Moves.Left);
        if (Grid.IsValid(leftPos) && !Grid.IsOccupied(leftPos)) legalMoves |= Moves.Left;

        var rightPos = Grid.GetNeighbor(headPosition, Moves.Right);
        if (Grid.IsValid(rightPos) && !Grid.IsOccupied(rightPos)) legalMoves |= Moves.Right;

        return legalMoves;
    }

    public void ApplySingleMove(byte move)
    {
        var me = Me;
        if (me.IsDead) return;

        var head = me.Head;
        var oldTail = me.Tail;

        // --- 1. L'ARENA PRENDE LE DECISIONI ---
        var newHead = Grid.GetNeighbor(head, move);

        if (!Grid.IsValid(newHead))
        {
            me.Kill();
            Grid.RemoveSnakeBody(me);
            return;
        }

        var hasEaten = Grid.IsFood(newHead);

        if (Grid.IsOccupied(newHead))
        {
            var isMovingOntoOwnVacatingTail = newHead == oldTail && !hasEaten;
            if (!isMovingOntoOwnVacatingTail)
            {
                me.Kill();
                Grid.RemoveSnakeBody(me);
                return;
            }
        }

        var damage = Grid.IsHazard(newHead) ? 10 : 1;
        var newTail = CalculateNewTailPosition(me, hasEaten); // Logica di gioco da implementare

        // --- 2. L'ARENA COMANDA AL CORPO (WARSNAKE) DI AGGIORNARSI ---
        me.UpdateAfterMove(newHead, newTail, hasEaten, damage);

        // --- 3. L'ARENA COMANDA AL MONDO (GRID) DI AGGIORNARSI ---
        Grid.UpdateSnakePosition(oldTail, newHead, hasEaten);
        if (hasEaten) Grid.RemoveFood(newHead);
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
        var up = Grid.GetNeighbor(oldTail, Moves.Up);
        if (Grid.IsValid(up) && snake.IsOnBody(up)) return up;

        var down = Grid.GetNeighbor(oldTail, Moves.Down);
        if (Grid.IsValid(down) && snake.IsOnBody(down)) return down;

        var left = Grid.GetNeighbor(oldTail, Moves.Left);
        if (Grid.IsValid(left) && snake.IsOnBody(left)) return left;

        var right = Grid.GetNeighbor(oldTail, Moves.Right);
        if (Grid.IsValid(right) && snake.IsOnBody(right)) return right;

        // Fallback: non dovrebbe mai succedere in un gioco normale,
        // ma per sicurezza restituiamo la vecchia coda.
        return oldTail;
    }
}