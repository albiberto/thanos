using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Thanos.MCST; // Assicurati che i tuoi 'using' siano corretti

namespace Thanos.War;

/// <summary>
/// Rappresenta la vista principale e l'API per interagire con uno stato di gioco completo.
/// È una ref struct sicura e ad alte prestazioni che opera su memoria pre-allocata.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public ref struct WarArena
{
    // --- CAMPI PRIVATI ---
    private WarField _field;
    private readonly Span<byte> _snakesMemory;
    private readonly int _snakeStride;
    private int _liveSnakesCount;

    /// <summary>
    /// Crea una nuova vista WarArena per uno stato di gioco esistente.
    /// </summary>
    public WarArena(WarField field, Span<byte> snakesMemory, int liveSnakesCount, int snakeStride)
    {
        _field = field;
        _snakesMemory = snakesMemory;
        _snakeStride = snakeStride;
        _liveSnakesCount = liveSnakesCount;
    }

    /// <summary>
    /// Fornisce accesso all'array di serpenti tramite un wrapper sicuro.
    /// </summary>
    public WarSnakeArray Snakes => new(_snakesMemory, _liveSnakesCount, _snakeStride);

    /// <summary>
    /// Calcola le mosse legali per tutti i serpenti e scrive i risultati (un byte per serpente) nello span fornito.
    /// </summary>
    public void GetLegalMovesForAll(Span<byte> legalMoveSets)
    {
        var snakes = Snakes;
        
        for (var i = 0; i < snakes.Length; i++)
        {
            var snake = snakes[i];
            legalMoveSets[i] = snake.Dead ? Moves.None : GetLegalMoves(snake);
        }
    }
    
    /// <summary>
    /// Calcola il set di mosse legali per un singolo serpente come maschera di bit.
    /// </summary>
    public byte GetLegalMoves(WarSnake snake)
    {
        var legalMoveSet = Moves.None;
        if (!_field.IsOccupied(_field.GetNeighbor(snake.Head, Moves.Up))) legalMoveSet |= Moves.Up;
        if (!_field.IsOccupied(_field.GetNeighbor(snake.Head, Moves.Down))) legalMoveSet |= Moves.Down;
        if (!_field.IsOccupied(_field.GetNeighbor(snake.Head, Moves.Left))) legalMoveSet |= Moves.Left;
        if (!_field.IsOccupied(_field.GetNeighbor(snake.Head, Moves.Right))) legalMoveSet |= Moves.Right;
        return legalMoveSet;
    }

    /// <summary>
    /// Simula un intero turno di gioco, date le mosse scelte (come bitmask) per ogni serpente.
    /// </summary>
    public void SimulateTurn(ReadOnlySpan<byte> chosenMoves)
    {
        var snakes = Snakes;
        var snakeCount = snakes.Length;

        scoped Span<ushort> newHeadPositions = stackalloc ushort[snakeCount];
        scoped Span<bool> hasEaten = stackalloc bool[snakeCount];
        scoped Span<bool> isDead = stackalloc bool[snakeCount];
        scoped Span<ushort> oldTailPositions = stackalloc ushort[snakeCount];

        // --- FASE 1: Preparazione ---
        for (var i = 0; i < snakeCount; i++)
        {
            var snake = snakes[i];
            if (snake.Dead) { isDead[i] = true; continue; }

            oldTailPositions[i] = snake.Tail;
            newHeadPositions[i] = _field.GetNeighbor(snake.Head, chosenMoves[i]);
        }

        // --- FASE 2: Risoluzione Conflitti ---
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

        // --- FASE 3: Esecuzione Movimento ---
        for (var i = 0; i < snakeCount; i++)
        {
            if (isDead[i]) continue;
            var snake = snakes[i];
            var hazardDamage = _field.IsHazard(newHeadPositions[i]) ? 15 : 0;
            snake.Move(newHeadPositions[i], hasEaten[i], hazardDamage);
        }

        // --- FASE 4: Aggiornamento Mondo ---
        for (var i = 0; i < snakeCount; i++)
        {
            var snake = snakes[i];
            if (!isDead[i] && snake.Dead) isDead[i] = true;

            if (isDead[i])
            {
                if (_liveSnakesCount > 0) _liveSnakesCount--;
                snake.GetSpans(out var span1, out var span2);
                foreach (var segment in span1) _field.Snakes.Clear(segment);
                foreach (var segment in span2) _field.Snakes.Clear(segment);
            }
            else
            {
                if (!hasEaten[i]) _field.Snakes.Clear(oldTailPositions[i]);
                _field.Snakes.Set(snake.Head);
            }
            if (hasEaten[i]) _field.Food.Clear(newHeadPositions[i]);
        }
    }

    /// <summary>
    /// Valuta lo stato finale del gioco dal punto di vista del nostro serpente (indice 0).
    /// </summary>
    /// <returns>1.0 per vittoria, -1.0 per sconfitta, 0.0 se il gioco continua.</returns>
    public float Evaluate()
    {
        if (Snakes[0].Dead) return -1.0f;
        return _liveSnakesCount <= 1 ? 1.0f : 0.0f;
    }

    /// <summary>
    /// Wrapper per l'array di serpenti che fornisce accesso indicizzato.
    /// </summary>
    public readonly ref struct WarSnakeArray(Span<byte> snakesMemory, int count, int stride)
    {
        private readonly Span<byte> _snakesMemory = snakesMemory;
        public int Length { get; } = count;

        /// <summary>
        /// Restituisce una "vista" WarSnake per il serpente all'indice specificato.
        /// </summary>
        public WarSnake this[int index]
        {
            get
            {
                var singleSnakeBlock = _snakesMemory.Slice(index * stride, stride);
                var headerSpan = singleSnakeBlock[..Unsafe.SizeOf<WarSnakeHeader>()];
                var bodySpan = MemoryMarshal.Cast<byte, ushort>(singleSnakeBlock[Unsafe.SizeOf<WarSnakeHeader>()..]);
                ref var header = ref MemoryMarshal.GetReference(MemoryMarshal.Cast<byte, WarSnakeHeader>(headerSpan));
                
                // Chiama il costruttore "vista"
                return new WarSnake(ref header, bodySpan);
            }
        }
    }
}