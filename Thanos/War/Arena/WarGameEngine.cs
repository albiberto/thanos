using Thanos.MCST;

namespace Thanos.War.Arena;

public static class WarGameEngine
{
    /// <summary>
    ///     Simula un intero turno di gioco, date le mosse scelte (come bitmask) per ogni serpente.
    /// </summary>
    public static void SimulateTurn(ReadOnlySpan<byte> chosenMoves)
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
    ///     Applica la mossa di un singolo serpente, aggiornando lo stato.
    ///     Usato per l'espansione dell'albero MCTS, è una versione semplificata di SimulateTurn.
    /// </summary>
    public static void ApplySingleMove(int snakeIndex, byte move)
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
    ///     Gestisce la logica completa per l'eliminazione di un serpente dallo stato del gioco.
    /// </summary>
    private static void KillSnake(int snakeIndex)
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
    ///     NUOVO: Calcola l'hash Zobrist iniziale per lo stato di gioco corrente.
    ///     Questo metodo va chiamato una sola volta quando si crea un nuovo stato dal server.
    /// </summary>
    public static void InitializeHash()
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
}