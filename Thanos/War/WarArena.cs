using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Thanos.MCST;

// Your using statements

namespace Thanos.War;

// CAMBIAMENTO CHIAVE: Non più 'unsafe', ma una 'ref struct' sicura.
[StructLayout(LayoutKind.Sequential)]
public ref struct WarArena
{
    // --- CAMPI PRIVATI (ora sicuri) ---
    private readonly WarContext _context;
    private WarField _field; // Contenuta direttamente per valore (essendo una ref struct)
    private readonly Span<byte> _snakesMemory; // Unico span per la memoria di tutti i serpenti
    private readonly int _snakeStride;
    private int _liveSnakesCount;

    /// <summary>
    ///     COSTRUTTORE MODERNO: Inizializza la vista sull'arena di gioco.
    ///     Sostituisce completamente il vecchio pattern 'PlacementNew'.
    /// </summary>
    public WarArena(ref WarField field, Span<byte> snakesMemory, in WarContext context, int snakeStride)
    {
        _field = field;
        _snakesMemory = snakesMemory;
        _context = context;
        _snakeStride = snakeStride;
        _liveSnakesCount = context.SnakeCount;
    }

    public WarSnakeArray Snakes => new(_snakesMemory, _context.SnakeCount, _snakeStride);

    /// <summary>
    /// Calcola le mosse legali per tutti i serpenti e scrive i risultati
    /// (un byte per serpente) nello span fornito.
    /// </summary>
    /// <summary>
    /// Calcola le mosse legali per tutti i serpenti e scrive i risultati
    /// (un byte per serpente) nello span fornito.
    /// </summary>
    public void GetLegalMovesForAll(Span<byte> legalMoveSets)
    {
        // Assicura che lo span fornito sia della dimensione corretta
        if (legalMoveSets.Length < _context.SnakeCount)
        {
            throw new ArgumentException("Provided span is too small.", nameof(legalMoveSets));
        }

        var snakes = this.Snakes;
        for (int i = 0; i < snakes.Length; i++)
        {
             var snake =  snakes[i];
            if (snake.Dead)
            {
                legalMoveSets[i] = Moves.None;
            }
            else
            {
                // Riutilizza il nostro metodo per il singolo serpente
                legalMoveSets[i] = GetLegalMoves(ref snake);
            }
        }
    }
    
    /// <summary>
    /// Calcola il set di mosse legali per un dato serpente e lo restituisce come maschera di bit (byte).
    /// Una mossa è legale se non porta a una casella già occupata (muri, ostacoli, o altri serpenti).
    /// La logica per non tornare indietro è gestita implicitamente da IsOccupied.
    /// </summary>
    public byte GetLegalMoves(ref WarSnake snake)
    {
        // Inizia con un set di mosse vuoto (tutti i bit a 0).
        byte legalMoveSet = Moves.None;
    
        // Controlla le 4 direzioni una per una.
        // GetNeighbor usa ancora l'enum MoveDirection, il che è corretto e leggibile.
    
        if (!_field.IsOccupied(_field.GetNeighbor(snake.Head, MoveDirection.Up)))
        {
            // Se la casella è libera, "accendi" il bit corrispondente alla mossa 'Up'.
            legalMoveSet |= Moves.Up;
        }
    
        if (!_field.IsOccupied(_field.GetNeighbor(snake.Head, MoveDirection.Down)))
        {
            legalMoveSet |= Moves.Down;
        }
    
        if (!_field.IsOccupied(_field.GetNeighbor(snake.Head, MoveDirection.Left)))
        {
            legalMoveSet |= Moves.Left;
        }
    
        if (!_field.IsOccupied(_field.GetNeighbor(snake.Head, MoveDirection.Right)))
        {
            legalMoveSet |= Moves.Right;
        }
    
        return legalMoveSet;
    }

    public void SimulateTurn(ReadOnlySpan<MoveDirection> allMoves)
    {
        var snakes = Snakes;
        var snakeCount = snakes.Length;

        // Pre-alloca tutto lo spazio necessario sullo stack
        scoped Span<ushort> newHeadPositions = stackalloc ushort[snakeCount];
        scoped Span<bool> hasEaten = stackalloc bool[snakeCount];
        scoped Span<bool> isDead = stackalloc bool[snakeCount];

        // --- FASE 1: Preparazione (ora molto più semplice) ---
        for (var i = 0; i < snakeCount; i++)
        {
            var snake = snakes[i];
            if (snake.Dead)
            {
                isDead[i] = true;
                continue;
            }

            // Non decidiamo più la mossa, la leggiamo dal parametro in input
            newHeadPositions[i] = _field.GetNeighbor(snake.Head, allMoves[i]);
        }

        // --- FASE 2: RISOLUZIONE DEI CONFLITTI (Chi si scontra? Chi mangia?) ---
        for (var i = 0; i < snakeCount; i++)
        {
            if (isDead[i]) continue;

            hasEaten[i] = _field.IsFood(newHeadPositions[i]);

            for (var j = i + 1; j < snakeCount; j++)
            {
                if (isDead[j]) continue;
                if (newHeadPositions[i] == newHeadPositions[j])
                {
                    var snakeA = snakes[i];
                    var snakeB = snakes[j];
                    if (snakeA.Length >= snakeB.Length) isDead[j] = true;
                    if (snakeB.Length >= snakeA.Length) isDead[i] = true;
                }
            }
        }

        // --- FASE 3: ESECUZIONE DEL MOVIMENTO (Aggiorna lo stato interno di ogni serpente) ---
        for (var i = 0; i < snakeCount; i++)
        {
            if (isDead[i]) continue;

            var snake = snakes[i];
            var hazardDamage = _field.IsHazard(newHeadPositions[i]) ? 15 : 0;

            // Il metodo 'Move' aggiorna la logica interna del serpente (posizione di testa/coda, vita, etc.)
            snake.Move(newHeadPositions[i], hasEaten[i], hazardDamage);
        }

        // --- FASE 4: AGGIORNAMENTO DEL MONDO (Aggiorna le bitboard in modo efficiente) ---
        for (var i = 0; i < snakeCount; i++)
        {
            var snake = snakes[i];

            // Controlla se il serpente è morto durante il movimento (es. per fame)
            if (!isDead[i] && snake.Dead) isDead[i] = true;

            if (isDead[i])
            {
                // Se il serpente è morto in questo turno, dobbiamo cancellare il suo vecchio corpo dalla mappa
                _liveSnakesCount--;
                snake.GetSpans(out var span1, out var span2);
                foreach (var segment in span1) _field.Snakes.Clear(segment);
                foreach (var segment in span2) _field.Snakes.Clear(segment);
            }
            else
            {
                // OTTIMIZZAZIONE: Aggiornamento chirurgico invece di cancellare e ridisegnare tutto
                if (!hasEaten[i]) _field.Snakes.Clear(oldTailPositions[i]); // Cancella solo la vecchia coda
                _field.Snakes.Set(snake.Head); // Aggiungi solo la nuova testa
            }

            // Aggiorna la mappa del cibo
            if (hasEaten[i]) _field.Food.Clear(newHeadPositions[i]);
        }
    }

    public float Evaluate()
    {
        if (Snakes[0].Dead) return -1.0f;
        return _liveSnakesCount <= 1
            ? 1.0f
            : 0.0f;
    }
    
    public int GetLegalMovesForSnake(ref WarSnake snake, Span<MoveDirection> legalMoves)
    {
        // Questo è il codice che prima era in GetLegalMoves, ma ora specifico per un serpente
        // ...
    }

    /// <summary>
    ///     TIPO ANNIDATO: Wrapper per l'array di serpenti, ora drasticamente più semplice.
    /// </summary>
    public readonly ref struct WarSnakeArray
    {
        private readonly Span<byte> _snakesMemory;
        private readonly int _stride;

        // Usa i tipi del costruttore primario direttamente
        public WarSnakeArray(Span<byte> snakesMemory, int count, int stride)
        {
            _snakesMemory = snakesMemory;
            Length = count;
            _stride = stride;
        }

        // CAMBIAMENTO 1: Restituisce 'WarSnake' per valore, non per 'ref'.
        public WarSnake this[int index]
        {
            get
            {
                // Prepara i pezzi di memoria come prima...
                var singleSnakeBlock = _snakesMemory.Slice(index * _stride, _stride);
                var headerSpan = singleSnakeBlock[..Unsafe.SizeOf<WarSnakeHeader>()];
                var bodySpan = MemoryMarshal.Cast<byte, ushort>(singleSnakeBlock[Unsafe.SizeOf<WarSnakeHeader>()..]);
                ref var header = ref MemoryMarshal.GetReference(MemoryMarshal.Cast<byte, WarSnakeHeader>(headerSpan));

                // CAMBIAMENTO 2: Chiama il nuovo costruttore semplice. Niente più codice 'Unsafe'.
                return new WarSnake(ref header, bodySpan);
            }
        }

        public int Length { get; }
    }
}