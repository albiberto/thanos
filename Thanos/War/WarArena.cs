using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Thanos.MCST;
using Thanos.War.Grid;
using Thanos.War.Snake;

// Assicurati che i tuoi 'using' siano corretti

namespace Thanos.War;

[StructLayout(LayoutKind.Sequential)]
public struct WarArenaHeader
{
    public int LiveSnakesCount;
    public long Hash;
}

/// <summary>
///     Rappresenta la vista principale e l'API per interagire con uno stato di gioco completo.
///     È una ref struct sicura e ad alte prestazioni che opera su memoria pre-allocata.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public ref struct WarArena
{
    // --- CAMPI PRIVATI ---
    private ref WarArenaHeader _header;
    private WarGrid _grid;
    private readonly Span<byte> _snakesMemory;
    private readonly Span<ushort> _newHeadPositions;
    private readonly Span<bool> _hasEaten;
    private readonly Span<bool> _isDead;
    private readonly Span<ushort> _oldTailPositions;
    private readonly int _snakeStride;

    /// <summary>
    ///     Crea una nuova vista WarArena per uno stato di gioco esistente.
    /// </summary>
    public WarArena(ref WarArenaHeader header,
        WarGrid grid,
        Span<byte> snakesMemory,
        Span<ushort> newHeadPositions,
        Span<bool> hasEaten,
        Span<bool> isDead,
        Span<ushort> oldTailPositions,
        int snakeStride)
    {
        _header = ref header;
        _grid = grid;
        _snakesMemory = snakesMemory;
        _newHeadPositions = newHeadPositions;
        _hasEaten = hasEaten;
        _isDead = isDead;
        _oldTailPositions = oldTailPositions;
        _snakeStride = snakeStride;
    }

    /// <summary>
    ///     Fornisce accesso all'array di serpenti tramite un wrapper sicuro.
    /// </summary>
    public WarSnakeArray Snakes => new(_snakesMemory, _header.LiveSnakesCount, _snakeStride);

    /// <summary>
    ///     NUOVO: Calcola l'hash Zobrist iniziale per lo stato di gioco corrente.
    ///     Questo metodo va chiamato una sola volta quando si crea un nuovo stato dal server.
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
            foreach (var segment in span1) hash ^= ZobristTable.GetSnakeValue(i, segment);
            foreach (var segment in span2) hash ^= ZobristTable.GetSnakeValue(i, segment);
        }

        _header.Hash = hash;
    }

    /// <summary>
    ///     NUOVO: Restituisce l'hash Zobrist corrente dello stato di gioco.
    /// </summary>
    public readonly long GetStateHash => _header.Hash;

    /// <summary>
    ///     Restituisce il set di mosse legali per un singolo serpente, rappresentato come maschera di bit.
    ///     Ottimizzazione: la logica è stata manualmente inlined per evitare chiamate a metodi aggiuntivi, migliorando le
    ///     prestazioni in un percorso critico ("hot path").
    ///     Nota: Sebbene la responsabilità di questa logica dovrebbe appartenere al WarField, è stata spostata direttamente
    ///     nella classe WarArena per motivi di performance.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte GetLegalMoves(WarSnake snake)
    {
        var head = snake.Head;
        var width = _grid.Width;
        var area = _grid.Area;
        var legalMoveSet = Moves.None;

        // --- Calcola e Controlla SU ---
        var upPos = head < width ? ushort.MaxValue : (ushort)(head - width);
        if (!_grid.IsOccupied(upPos)) legalMoveSet |= Moves.Up;

        // --- Calcola e Controlla GIÙ ---
        var downPos = head >= area - width ? ushort.MaxValue : (ushort)(head + width);
        if (!_grid.IsOccupied(downPos)) legalMoveSet |= Moves.Down;

        // --- Calcola e Controlla SINISTRA ---
        var leftPos = head % width == 0 ? ushort.MaxValue : (ushort)(head - 1);
        if (!_grid.IsOccupied(leftPos)) legalMoveSet |= Moves.Left;

        // --- Calcola e Controlla DESTRA ---
        var rightPos = (head + 1) % width == 0 ? ushort.MaxValue : (ushort)(head + 1);
        if (!_grid.IsOccupied(rightPos)) legalMoveSet |= Moves.Right;

        return legalMoveSet;
    }

    /// <summary>
    ///     Simula un intero turno di gioco, date le mosse scelte (come bitmask) per ogni serpente.
    /// </summary>
    /// <summary>
    ///     Simula un intero turno di gioco. Questa versione è a zero-allocazioni,
    ///     utilizzando un buffer pre-allocato ("workspace") per i dati temporanei.
    /// </summary>
    public void SimulateTurn(ReadOnlySpan<byte> chosenMoves)
    {
        var snakes = Snakes;
        var snakeCount = snakes.Length;
        ref var hash = ref _header.Hash;

        // Pulisce il buffer isDead per questo nuovo turno.
        _isDead.Clear();

        // Le 4 fasi ora usano direttamente i campi privati.
        // --- FASE 1: Preparazione ---
        for (var i = 0; i < snakeCount; i++)
        {
            var snake = snakes[i];
            if (snake.Dead)
            {
                _isDead[i] = true;
                continue;
            }

            _oldTailPositions[i] = snake.Tail;
            var head = snake.Head;
            var move = chosenMoves[i];

            _newHeadPositions[i] = move switch
            {
                Moves.Up => head < _grid.Width ? ushort.MaxValue : (ushort)(head - _grid.Width),
                Moves.Down => head >= _grid.Area - _grid.Width ? ushort.MaxValue : (ushort)(head + _grid.Width),
                Moves.Left => head % _grid.Width == 0 ? ushort.MaxValue : (ushort)(head - 1),
                Moves.Right => (head + 1) % _grid.Width == 0 ? ushort.MaxValue : (ushort)(head + 1),
                _ => ushort.MaxValue
            };
        }

        // --- FASE 2: Risoluzione Conflitti ---
        // ... (questa fase è identica a prima)
        for (var i = 0; i < snakeCount; i++)
        {
            if (_isDead[i]) continue;
            if (_grid.IsOccupied(_newHeadPositions[i]))
            {
                _isDead[i] = true;
                continue;
            }

            _hasEaten[i] = _grid.IsFood(_newHeadPositions[i]);
            for (var j = i + 1; j < snakeCount; j++)
            {
                if (_isDead[j]) continue;
                if (_newHeadPositions[i] == _newHeadPositions[j])
                {
                    // TODO: craere metodo compare interno a warsnake?
                    var snakeA = snakes[i];
                    var snakeB = snakes[j];
                    if (snakeA.Length >= snakeB.Length) _isDead[j] = true;
                    if (snakeB.Length >= snakeA.Length) _isDead[i] = true;
                }
            }
        }

        // --- FASE 3: Esecuzione Movimento ---
        // ... (questa fase è identica a prima)
        for (var i = 0; i < snakeCount; i++)
        {
            if (_isDead[i]) continue;
            var snake = snakes[i];
            var hazardDamage = _grid.IsHazard(_newHeadPositions[i]) ? 15 : 0;
            var totalDamage = 1 + hazardDamage;
            snake.Move(_newHeadPositions[i], _hasEaten[i], totalDamage);
        }

        // --- FASE 4: Aggiornamento Mondo ---
        // ... (questa fase è identica a prima)
        for (var i = 0; i < snakeCount; i++)
        {
            var wasAlive = !_isDead[i];
            if (wasAlive && snakes[i].Dead) _isDead[i] = true;
            if (_isDead[i] && wasAlive)
            {
                _header.LiveSnakesCount--;
                var deadSnake = snakes[i];
                deadSnake.GetSpans(out var span1, out var span2);
                foreach (var segment in span1)
                {
                    hash ^= ZobristTable.GetSnakeValue(i, segment);
                    _grid.Snakes.Clear(segment);
                }

                foreach (var segment in span2)
                {
                    hash ^= ZobristTable.GetSnakeValue(i, segment);
                    _grid.Snakes.Clear(segment);
                }
            }
            else if (wasAlive)
            {
                hash ^= ZobristTable.GetSnakeValue(i, _newHeadPositions[i]);
                _grid.Snakes.Set(_newHeadPositions[i]);
                if (!_hasEaten[i])
                {
                    hash ^= ZobristTable.GetSnakeValue(i, _oldTailPositions[i]);
                    _grid.Snakes.Clear(_oldTailPositions[i]);
                }
            }

            if (_hasEaten[i]) _grid.Food.Clear(_newHeadPositions[i]);
        }
    }

    /// <summary>
    ///     Valuta lo stato finale del gioco dal punto di vista del nostro serpente (indice 0).
    /// </summary>
    /// <returns>1.0 per vittoria, -1.0 per sconfitta, 0.0 se il gioco continua.</returns>
    public float Evaluate()
    {
        if (Snakes[0].Dead) return -1.0f;
        return _header.LiveSnakesCount <= 1 ? 1.0f : 0.0f;
    }

    /// <summary>
    ///     Gestisce la logica completa per l'eliminazione di un serpente dallo stato del gioco.
    /// </summary>
    private void KillSnake(int snakeIndex)
    {
        var snake = Snakes[snakeIndex];
        // Se era già stato segnato come morto in una fase precedente, non fare nulla
        if (snake.Dead) return;

        snake.Kill(); // Imposta la vita a 0
        _header.LiveSnakesCount--;

        // Rimuovi il serpente dalla bitboard e aggiorna l'hash
        ref var hash = ref _header.Hash;
        snake.GetSpans(out var span1, out var span2);
        foreach (var segment in span1)
        {
            hash ^= ZobristTable.GetSnakeValue(snakeIndex, segment);
            _grid.Snakes.Clear(segment);
        }

        foreach (var segment in span2)
        {
            hash ^= ZobristTable.GetSnakeValue(snakeIndex, segment);
            _grid.Snakes.Clear(segment);
        }
    }

    /// <summary>
    ///     Applica la mossa di un singolo serpente, aggiornando lo stato.
    ///     Usato per l'espansione dell'albero MCTS, è una versione semplificata di SimulateTurn.
    /// </summary>
    public void ApplySingleMove(int snakeIndex, byte move)
    {
        var snake = Snakes[snakeIndex];
        if (snake.Dead) return;

        // 1. Calcola la nuova posizione della testa e la vecchia coda
        var oldTail = snake.Tail;
        var head = snake.Head;
        var newHead = move switch
        {
            Moves.Up => head < _grid.Width ? ushort.MaxValue : (ushort)(head - _grid.Width),
            Moves.Down => head >= _grid.Area - _grid.Width ? ushort.MaxValue : (ushort)(head + _grid.Width),
            Moves.Left => head % _grid.Width == 0 ? ushort.MaxValue : (ushort)(head - 1),
            Moves.Right => (head + 1) % _grid.Width == 0 ? ushort.MaxValue : (ushort)(head + 1),
            _ => ushort.MaxValue
        };

        // 2. Controlla se la mossa porta a morte istantanea (muro o corpo di un altro serpente)
        if (_grid.IsOccupied(newHead))
        {
            KillSnake(snakeIndex);
            return; // L'espansione finisce qui in un nodo terminale
        }

        // 3. Controlla cibo e calcola il danno
        var hasEaten = _grid.IsFood(newHead);
        var hazardDamage = _grid.IsHazard(newHead) ? 15 : 0;
        var totalDamage = 1 + hazardDamage;

        // 4. Aggiorna lo stato interno del serpente
        snake.Move(newHead, hasEaten, totalDamage);

        // Controlla se il serpente è morto per fame/danni
        if (snake.Dead)
        {
            KillSnake(snakeIndex);
            return;
        }

        // 5. Aggiorna lo stato del mondo (bitboard e hash)
        ref var hash = ref _header.Hash;

        // Aggiungi la nuova testa
        _grid.Snakes.Set(newHead);
        hash ^= ZobristTable.GetSnakeValue(snakeIndex, newHead);

        // Rimuovi la vecchia coda (se non ha mangiato)
        if (!hasEaten)
        {
            _grid.Snakes.Clear(oldTail);
            hash ^= ZobristTable.GetSnakeValue(snakeIndex, oldTail);
        }
        else // Se ha mangiato, rimuovi il cibo dalla bitboard
        {
            _grid.Food.Clear(newHead);
        }
    }

    /// <summary>
    ///     Wrapper per l'array di serpenti che fornisce accesso indicizzato.
    /// </summary>
    public readonly ref struct WarSnakeArray(Span<byte> snakesMemory, int count, int stride)
    {
        private readonly Span<byte> _snakesMemory = snakesMemory;
        public int Length { get; } = count;

        /// <summary>
        ///     Restituisce una "vista" WarSnake per il serpente all'indice specificato.
        /// </summary>
        public WarSnake this[int index]
        {
            get
            {
                var singleSnakeBlock = _snakesMemory.Slice(index * stride, stride);
                var headerSpan = singleSnakeBlock[..Unsafe.SizeOf<Health>()];
                var bodySpan = MemoryMarshal.Cast<byte, ushort>(singleSnakeBlock[Unsafe.SizeOf<Health>()..]);
                ref var profile = ref MemoryMarshal.GetReference(MemoryMarshal.Cast<byte, Health>(headerSpan));
                
                // TODO: correggi offsets
                ref var anatomy = ref MemoryMarshal.GetReference(MemoryMarshal.Cast<byte, Anatomy>(headerSpan));

                // Chiama il costruttore "vista"
                return new WarSnake(ref profile, ref anatomy, bodySpan);
            }
        }
    }
}