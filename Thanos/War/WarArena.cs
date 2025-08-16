using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Thanos.MCST; // Your using statements

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
    /// COSTRUTTORE MODERNO: Inizializza la vista sull'arena di gioco.
    /// Sostituisce completamente il vecchio pattern 'PlacementNew'.
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

    public int GetLegalMoves(Span<MoveDirection> legalMoves)
    {
        var moveCount = 0;
        var me = Snakes[0];
        if (me.Dead) return 0;

        for (var i = 0; i < 4; i++)
        {
            var direction = (MoveDirection)i;
            // CAMBIAMENTO CHIAVE: Accesso diretto a _field, non più un puntatore '_arena._field->'
            var newHeadPos = _field.GetNeighbor(me.Head, direction);

            if (!_field.IsOccupied(newHeadPos))
            {
                legalMoves[moveCount++] = direction;
            }
        }
        return moveCount;
    }
    
    public void SimulateTurn(MoveDirection myMove)
    {
        var snakes = this.Snakes;
        var snakeCount = (int)snakes.Length;

        Span<MoveDirection> moves = stackalloc MoveDirection[snakeCount];
        Span<ushort> newHeadPositions = stackalloc ushort[snakeCount];
        Span<bool> hasEaten = stackalloc bool[snakeCount];
        Span<bool> isDead = stackalloc bool[snakeCount];

        // FASE 1: Preparazione
        for (var i = 0; i < snakeCount; i++)
        {
            var snake = snakes[i];
            if (snake.Dead) { isDead[i] = true; continue; }

            moves[i] = (i == 0) ? myMove : GetSimpleMove(ref snake);
            newHeadPositions[i] = _field.GetNeighbor(snake.Head, moves[i]);
        }

        // FASE 2: Risoluzione Cibo e Collisioni
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

        // FASE 3: Movimento
        for (var i = 0; i < snakeCount; i++)
        {
            if (isDead[i]) continue;
            var snake = snakes[i];
            var hazardDamage = _field.IsHazard(newHeadPositions[i]) ? 15 : 0;
            snake.Move(newHeadPositions[i], hasEaten[i], hazardDamage);
        }

        // FASE 4: Aggiornamento Finale
        _field.Snakes.ClearAll();
        for (var i = 0; i < snakeCount; i++)
        {
            var snake = snakes[i];
            if (isDead[i] || snake.Dead)
            {
                if (!isDead[i]) { isDead[i] = true; _liveSnakesCount--; }
                continue;
            }
            // Ridisegna il serpente sulla bitboard
            snake.GetSpans(out var span1, out var span2);
            foreach (var segment in span1) _field.Snakes.Set(segment);
            foreach (var segment in span2) _field.Snakes.Set(segment);
            _field.Snakes.Set(snake.Head);
        }
        for (var i = 0; i < snakeCount; i++)
            if (hasEaten[i])
                _field.Food.Clear(newHeadPositions[i]);
    }

    public float Evaluate()
    {
        if (Snakes[0].Dead) return -1.0f;
        return _liveSnakesCount <= 1 
            ? 1.0f 
            : 0.0f;
    }

    private MoveDirection GetSimpleMove(ref WarSnake snake)
    {
        if (snake.Dead) return MoveDirection.Up;
        for (var i = 0; i < 4; i++)
        {
            var direction = (MoveDirection)i;
            var newHeadPos = _field.GetNeighbor(snake.Head, direction);
            if (!_field.IsOccupied(newHeadPos)) return direction;
        }
        return MoveDirection.Up;
    }

    /// <summary>
    /// TIPO ANNIDATO: Wrapper per l'array di serpenti, ora drasticamente più semplice.
    /// </summary>
    public readonly ref struct WarSnakeArray
    {
        private readonly Span<byte> _snakesMemory;
        private readonly int _count;
        private readonly int _stride;

        // Usa i tipi del costruttore primario direttamente
        public WarSnakeArray(Span<byte> snakesMemory, int count, int stride)
        {
            _snakesMemory = snakesMemory;
            _count = count;
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
        public int Length => _count;
    }
}