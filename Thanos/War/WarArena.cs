using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Thanos.MCST; // Assicurati che i tuoi 'using' siano corretti

namespace Thanos.War;

[StructLayout(LayoutKind.Sequential)]
public struct WarArenaHeader
{
    public int LiveSnakesCount;
    public long ZobristHash;
}

/// <summary>
/// Rappresenta la vista principale e l'API per interagire con uno stato di gioco completo.
/// È una ref struct sicura e ad alte prestazioni che opera su memoria pre-allocata.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public ref struct WarArena
{
    // --- CAMPI PRIVATI ---
    private ref WarArenaHeader _header;
    private WarField _field;
    private readonly Span<byte> _snakesMemory;
    private readonly int _snakeStride;

    /// <summary>
    /// Crea una nuova vista WarArena per uno stato di gioco esistente.
    /// </summary>
    public WarArena(ref WarArenaHeader header, WarField field, Span<byte> snakesMemory, int snakeStride)
    {
        _header = ref header;
        _field = field;
        _snakesMemory = snakesMemory;
        _snakeStride = snakeStride;
    }

    /// <summary>
    /// Fornisce accesso all'array di serpenti tramite un wrapper sicuro.
    /// </summary>
    public WarSnakeArray Snakes => new(_snakesMemory, _header.LiveSnakesCount, _snakeStride);

    /// <summary>
    /// NUOVO: Calcola l'hash Zobrist iniziale per lo stato di gioco corrente.
    /// Questo metodo va chiamato una sola volta quando si crea un nuovo stato dal server.
    /// </summary>
    public void InitializeHash()
    {
        long hash = 0;
        var snakes = Snakes;
        for (var i = 0; i < snakes.Length; i++)
        {
            var snake = snakes[i];
            if (snake.Dead) continue;
            
            // Ottieni i segmenti del corpo del serpente
            snake.GetSpans(out var span1, out var span2);
            
            // Applica l'operazione XOR per ogni segmento del corpo
            foreach (var segment in span1)
            {
                hash ^= ZobristTable.GetSnakeValue(i, segment);
            }
            foreach (var segment in span2)
            {
                hash ^= ZobristTable.GetSnakeValue(i, segment);
            }
        }
        _header.ZobristHash = hash;
    }
    
    /// <summary>
    /// NUOVO: Restituisce l'hash Zobrist corrente dello stato di gioco.
    /// </summary>
    public readonly long GetStateHash() => _header.ZobristHash;
    
    /// <summary>
    /// Restituisce il set di mosse legali per un singolo serpente, rappresentato come maschera di bit.
    /// 
    /// Ottimizzazione: la logica è stata manualmente inlined per evitare chiamate a metodi aggiuntivi, migliorando le prestazioni in un percorso critico ("hot path").
    /// 
    /// Nota: Sebbene la responsabilità di questa logica dovrebbe appartenere al WarField, è stata spostata direttamente nella classe WarArena per motivi di performance.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte GetLegalMoves(WarSnake snake)
    {
        var head = snake.Head;
        var width = _field.Width;
        var area = _field.Area;
        var legalMoveSet = Moves.None;

        // --- Calcola e Controlla SU ---
        var upPos = head < width ? ushort.MaxValue : (ushort)(head - width);
        if (!_field.IsOccupied(upPos)) 
        {
            legalMoveSet |= Moves.Up;
        }

        // --- Calcola e Controlla GIÙ ---
        var downPos = head >= area - width ? ushort.MaxValue : (ushort)(head + width);
        if (!_field.IsOccupied(downPos))
        {
            legalMoveSet |= Moves.Down;
        }

        // --- Calcola e Controlla SINISTRA ---
        var leftPos = head % width == 0 ? ushort.MaxValue : (ushort)(head - 1);
        if (!_field.IsOccupied(leftPos))
        {
            legalMoveSet |= Moves.Left;
        }

        // --- Calcola e Controlla DESTRA ---
        var rightPos = (head + 1) % width == 0 ? ushort.MaxValue : (ushort)(head + 1);
        if (!_field.IsOccupied(rightPos))
        {
            legalMoveSet |= Moves.Right;
        }

        return legalMoveSet;
    }

    /// <summary>
    /// Simula un intero turno di gioco, date le mosse scelte (come bitmask) per ogni serpente.
    /// </summary>
    public void SimulateTurn(ReadOnlySpan<byte> chosenMoves)
    {
        var snakes = Snakes;
        var snakeCount = snakes.Length;
        ref var hash = ref _header.ZobristHash; // Ottieni un riferimento per modificare l'hash

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
            var head = snake.Head;
            var move = chosenMoves[i];

            // Logica di GetNeighbor copiata qui per massime prestazioni
            newHeadPositions[i] = move switch
            {
                Moves.Up => head < _field.Width ? ushort.MaxValue : (ushort)(head - _field.Width),
                Moves.Down => head >= _field.Area - _field.Width ? ushort.MaxValue : (ushort)(head + _field.Width),
                Moves.Left => head % _field.Width == 0 ? ushort.MaxValue : (ushort)(head - 1),
                Moves.Right => (head + 1) % _field.Width == 0 ? ushort.MaxValue : (ushort)(head + 1),
                _ => ushort.MaxValue
            };
        }

        // --- FASE 2: Risoluzione Conflitti ---
        for (var i = 0; i < snakeCount; i++)
        {
            if (isDead[i]) continue;

            // IsOccupied controlla già i corpi degli altri serpenti e i muri (tramite ushort.MaxValue)
            if (_field.IsOccupied(newHeadPositions[i]))
            {
                isDead[i] = true;
                continue; // Questo serpente morirà, non serve controllare altro
            }
            
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
            var totalDamage = 1 + hazardDamage; // <- CORREZIONE: Aggiunge il danno base;
            snake.Move(newHeadPositions[i], hasEaten[i], totalDamage);
        }

        // --- FASE 4: Aggiornamento Mondo ---
        for (var i = 0; i < snakeCount; i++)
        {
            // Controlla se un serpente era vivo e ora è morto
            var wasAlive = !isDead[i];
            if (wasAlive && snakes[i].Dead)
            {
                isDead[i] = true;
            }

            if (isDead[i] && wasAlive) // Serpente appena morto in questo turno
            {
                _header.LiveSnakesCount--; 
                
                // Aggiornamento Hash: Rimuovi il corpo del serpente morto
                var deadSnake = snakes[i];
                deadSnake.GetSpans(out var span1, out var span2);
                foreach (var segment in span1)
                {
                    hash ^= ZobristTable.GetSnakeValue(i, segment);
                    _field.Snakes.Clear(segment);
                }
                foreach (var segment in span2)
                {
                    hash ^= ZobristTable.GetSnakeValue(i, segment);
                    _field.Snakes.Clear(segment);
                }
            }
            else if (wasAlive) // Serpente ancora vivo
            {
                // Aggiornamento Hash: Aggiungi la nuova testa
                hash ^= ZobristTable.GetSnakeValue(i, newHeadPositions[i]);
                _field.Snakes.Set(newHeadPositions[i]);
                
                if (!hasEaten[i])
                {
                    // Aggiornamento Hash: Rimuovi la vecchia coda
                    hash ^= ZobristTable.GetSnakeValue(i, oldTailPositions[i]);
                    _field.Snakes.Clear(oldTailPositions[i]);
                }
            }

            // L'hash del cibo non viene tracciato, quindi qui non cambia nulla
            if (hasEaten[i])
            {
                _field.Food.Clear(newHeadPositions[i]);
            }
        }
    }

    /// <summary>
    /// Valuta lo stato finale del gioco dal punto di vista del nostro serpente (indice 0).
    /// </summary>
    /// <returns>1.0 per vittoria, -1.0 per sconfitta, 0.0 se il gioco continua.</returns>
    public float Evaluate()
    {
        if (Snakes[0].Dead) return -1.0f;
        return _header.LiveSnakesCount <= 1 ? 1.0f : 0.0f;
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