using Thanos.Common;
using Thanos.SourceGen;

namespace Thanos.War;

/// <summary>
///     Rappresenta una singola istanza di gioco. È il cervello che orchestra la logica.
/// </summary>
public readonly ref struct Arena(SnakesSystem system, Span<byte> food, Span<byte> hazards, Span<byte> allSnakes, ReadOnlySpan<ushort> neighbors, Dictionary<Guid, int> map, int area)
{
    private readonly SnakesSystem _system = system;
    private readonly Grid _grid = new(area, food, hazards, allSnakes, neighbors);

    /// <summary>
    ///     Inizializza lo stato dell'arena (usando "Placement New")
    ///     basandosi su una richiesta di configurazione di gioco.
    /// </summary>
    public void InitializeFromRequest(in Request request)
    {
        // --- FASE 1: Pulizia Totale dello Stato ---
        _grid.Food.Clear();
        _grid.Hazards.Clear();
        _grid.Snakes.Clear(); // Pulisce il bitboard combinato

        // Uccide tutti i serpenti per creare una "tabula rasa".
        // Questo gestisce correttamente i serpenti che sono morti nel turno precedente.
        for (var i = 0; i < _system.Count; i++) _system[i].Kill();

        var board = request.Board;

        // --- FASE 2: Inizializzazione e Sincronizzazione in un Unico Passaggio ---

        // Posiziona "Me"
        if (map.TryGetValue(request.You.Id, out var meIndex))
        {
            var me = _system[meIndex];
            me.Initialize(request.You.Health, request.You.Body);
        
            _grid.Snakes.Or(me.Body);
        }

        // Posiziona gli avversari
        foreach (var enemyData in board.Snakes)
        {
            if (enemyData.Id == request.You.Id || !map.TryGetValue(enemyData.Id, out var enemyIndex)) continue;
            
            var enemy = _system[enemyIndex];
            enemy.Initialize(enemyData.Health, enemyData.Body);

            _grid.Snakes.Or(enemy.Body);
        }

        // --- FASE 3: Posizionamento di Cibo e Ostacoli ---
        // Questa parte rimane invariata.
        foreach (var foodPosition in board.Food) _grid.Food.Set(foodPosition);
        foreach (var hazardPosition in board.Hazards) _grid.Hazards.Set(hazardPosition);
    }

    /// <summary>
    ///     Clona lo stato di un'altra Arena in questa istanza.
    ///     Operazione estremamente veloce basata su una copia di memoria.
    /// </summary>
    public void CloneFrom(in Arena source) => source._system.Raw.CopyTo(_system.Raw);

    private WarSnake Me => _system.Me;
    public bool GameOver => Me.IsDead;

    public byte GetLegalMoves() => GetLegalMoves(Me.Head);

    public byte GetLegalMoves(ushort headPosition)
    {
        byte legalMoves = 0;

        // I controlli ora usano la Grid in modo coerente
        var upPos = _grid.GetNeighbor(headPosition, Moves.Up);
        if (Grid.IsValid(upPos) && !_grid.IsOccupied(upPos)) legalMoves |= Moves.Up;

        var downPos = _grid.GetNeighbor(headPosition, Moves.Down);
        if (Grid.IsValid(downPos) && !_grid.IsOccupied(downPos)) legalMoves |= Moves.Down;

        var leftPos = _grid.GetNeighbor(headPosition, Moves.Left);
        if (Grid.IsValid(leftPos) && !_grid.IsOccupied(leftPos)) legalMoves |= Moves.Left;

        var rightPos = _grid.GetNeighbor(headPosition, Moves.Right);
        if (Grid.IsValid(rightPos) && !_grid.IsOccupied(rightPos)) legalMoves |= Moves.Right;

        return legalMoves;
    }

    public void ApplySingleMove(byte move)
    {
        var me = Me;
        if (me.IsDead) return;

        var head = me.Head;
        var oldTail = me.Tail;

        // --- 1. L'ARENA PRENDE LE DECISIONI ---
        var newHead = _grid.GetNeighbor(head, move);

        if (!Grid.IsValid(newHead))
        {
            me.Kill();
            _grid.RemoveSnakeBody(me);
            return;
        }

        var hasEaten = _grid.IsFood(newHead);

        if (_grid.IsOccupied(newHead))
        {
            var isMovingOntoOwnVacatingTail = newHead == oldTail && !hasEaten;
            if (!isMovingOntoOwnVacatingTail)
            {
                me.Kill();
                _grid.RemoveSnakeBody(me);
                return;
            }
        }

        var damage = _grid.IsHazard(newHead) ? 10 : 1;
        var newTail = CalculateNewTailPosition(me, hasEaten); // Logica di gioco da implementare

        // --- 2. L'ARENA COMANDA AL CORPO (WARSNAKE) DI AGGIORNARSI ---
        me.UpdateAfterMove(newHead, newTail, hasEaten, damage);

        // --- 3. L'ARENA COMANDA AL MONDO (GRID) DI AGGIORNARSI ---
        _grid.UpdateSnakePosition(oldTail, newHead, hasEaten);
        if (hasEaten) _grid.RemoveFood(newHead);
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
        var up = _grid.GetNeighbor(oldTail, Moves.Up);
        if (Grid.IsValid(up) && snake.IsOnBody(up)) return up;

        var down = _grid.GetNeighbor(oldTail, Moves.Down);
        if (Grid.IsValid(down) && snake.IsOnBody(down)) return down;

        var left = _grid.GetNeighbor(oldTail, Moves.Left);
        if (Grid.IsValid(left) && snake.IsOnBody(left)) return left;

        var right = _grid.GetNeighbor(oldTail, Moves.Right);
        if (Grid.IsValid(right) && snake.IsOnBody(right)) return right;

        // Fallback: non dovrebbe mai succedere in un gioco normale,
        // ma per sicurezza restituiamo la vecchia coda.
        return oldTail;
    }

    public float Outcome()
    {
        if (Me.IsDead) return -1.0f;

        return Me.Length >= _grid.Area
            ? 1.0f
            : 0.0f;
    }
}