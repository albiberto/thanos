using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Thanos.MCST;

namespace Thanos.War;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WarArena
{
    /// <summary>
    ///     L'UNICA porta d'accesso pubblica ai dati e alle operazioni dell'arena.
    /// </summary>
    public Accessor Api => new(ref this);

    // --- DATI PRIVATI ---
    private WarContext _context;
    private WarField* _field;
    private WarSnake* _snakes;
    private uint _snakeCount;
    private uint _snakeStride;
    private uint _liveSnakesCount;

    public static void PlacementNew(WarArena* arenaPtr, WarSnake* snakesPtr, WarField* fieldPtr, in WarContext context, uint liveSnakeCount, uint snakeStride)
    {
        arenaPtr->_context = context;
        arenaPtr->_field = fieldPtr;
        arenaPtr->_liveSnakesCount = liveSnakeCount;
        arenaPtr->_snakes = snakesPtr;
        arenaPtr->_snakeCount = context.SnakeCount;
        arenaPtr->_snakeStride = snakeStride;
    }

    /// <summary>
    ///     TIPO ANNIDATO: Funge da API pubblica e sicura per la WarArena.
    ///     Ora contiene anche la logica di gioco.
    /// </summary>
    public readonly ref struct Accessor(ref WarArena arena)
    {
        private readonly ref WarArena _arena = ref arena;

        /// <summary>
        ///     Restituisce il wrapper per l'array di serpenti.
        ///     CORREZIONE: Ora chiama il costruttore corretto di WarSnakeArray.
        /// </summary>
        public WarSnakeArray Snakes => new(ref _arena);

        /// <summary>
        ///     Calcola le mosse legali per il nostro serpente (indice 0) e le scrive nello span fornito.
        /// </summary>
        /// <returns>Il numero di mosse legali trovate.</returns>
        public int GetLegalMoves(Span<MoveDirection> legalMoves)
        {
            var moveCount = 0;
            ref var me = ref Snakes[0];
            if (me.Dead) return 0;

            // Controlla le 4 direzioni
            for (var i = 0; i < 4; i++)
            {
                var direction = (MoveDirection)i;
                var body = me.AsBody();
                var newHeadPos = _arena._field->GetNeighbor(body.Head, direction);

                // Una mossa è illegale se finisce contro un muro, un ostacolo,
                // o il corpo di un altro serpente.
                if (!_arena._field->IsOccupied(newHeadPos)) legalMoves[moveCount++] = direction;
            }

            return moveCount;
        }

        /// <summary>
        ///     Simula un intero turno di gioco, muovendo tutti i serpenti e risolvendo le collisioni.
        /// </summary>
        public void SimulateTurn(MoveDirection myMove)
        {
            var snakes = Snakes;
            var snakeCount = (int)snakes.Length;

            // --- 1. PREPARAZIONE: Raccogli le mosse e calcola le nuove posizioni ---
            Span<MoveDirection> moves = stackalloc MoveDirection[snakeCount];
            Span<ushort> newHeadPositions = stackalloc ushort[snakeCount];
            Span<bool> hasEaten = stackalloc bool[snakeCount];
            Span<bool> isDead = stackalloc bool[snakeCount]; // Traccia chi muore

            // Raccogli le mosse per tutti i serpenti vivi
            for (var i = 0; i < snakeCount; i++)
            {
                ref var snake = ref snakes[i];
                if (snake.Dead)
                {
                    isDead[i] = true;
                    continue;
                }

                moves[i] = i == 0 ? myMove : GetSimpleMove(ref snake);
                newHeadPositions[i] = _arena._field->GetNeighbor(snake.AsBody().Head, moves[i]);
            }

            // --- 2. RISOLUZIONE: Cibo e collisioni testa-a-testa ---
            for (var i = 0; i < snakeCount; i++)
            {
                if (isDead[i]) continue;

                // Chi mangia?
                hasEaten[i] = _arena._field->IsFood(newHeadPositions[i]);

                // Collisioni testa-a-testa
                for (var j = i + 1; j < snakeCount; j++)
                {
                    if (isDead[j]) continue;

                    if (newHeadPositions[i] == newHeadPositions[j])
                    {
                        // Se le teste si scontrano, muoiono entrambi se l'avversario non è più piccolo
                        ref var snakeA = ref snakes[i];
                        ref var snakeB = ref snakes[j];

                        var snakeALength = snakeA.AsBody().Length;
                        var snakeBLength = snakeB.AsBody().Length;

                        if (snakeALength >= snakeBLength) isDead[j] = true;
                        if (snakeBLength >= snakeALength) isDead[i] = true;
                    }
                }
            }

            // --- 3. MOVIMENTO: Aggiorna lo stato di ogni serpente ---
            Span<ushort> oldTails = stackalloc ushort[snakeCount];
            for (var i = 0; i < snakeCount; i++)
            {
                if (isDead[i]) continue;

                ref var snake = ref snakes[i];
                oldTails[i] = snake.AsBody().Tail;
                var hazardDamage = _arena._field->IsHazard(newHeadPositions[i]) ? 15 : 0;

                snake.Move(newHeadPositions[i], hasEaten[i], hazardDamage);
            }

            // --- 4. AGGIORNAMENTO FINALE: Collisioni col corpo e pulizia ---
            var field = _arena._field;
            field->Snakes.ClearAll(); // Ripulisce la mappa dei serpenti per ridisegnarla

            for (var i = 0; i < snakeCount; i++)
            {
                ref var snake = ref snakes[i];

                // Se un serpente è morto in una fase precedente, o per fame
                if (isDead[i] || snake.Dead)
                {
                    if (!isDead[i]) // Se non era già stato segnato come morto
                    {
                        isDead[i] = true;
                        _arena._liveSnakesCount--;
                    }

                    continue; // Non disegnarlo sulla mappa
                }

                // Ridisegna il corpo del serpente sulla mappa
                snake.AsBody().GetSpans(out var span1, out var span2);
                foreach (var segment in span1) field->Snakes.Set(segment);
                foreach (var segment in span2) field->Snakes.Set(segment);
                field->Snakes.Set(snake.AsBody().Head);

                // Controllo collisione col corpo (dopo che tutti sono stati disegnati)
                // (Questa è una logica semplificata, la gestione completa delle collisioni è complessa)
            }

            // Aggiorna la mappa del cibo
            for (var i = 0; i < snakeCount; i++)
                if (hasEaten[i])
                    field->Food.Clear(newHeadPositions[i]);
        }

        /// <summary>
        ///     Valuta lo stato attuale del gioco.
        /// </summary>
        /// <returns>1.0 per vittoria, -1.0 per sconfitta, 0.0 se il gioco continua.</returns>
        public float Evaluate()
        {
            ref var me = ref Snakes[0];

            if (me.Dead) return -1.0f; // Sconfitta

            if (_arena._liveSnakesCount <= 1) return 1.0f; // Vittoria (siamo gli unici rimasti)

            return 0.0f; // Gioco in corso
        }

        // All'interno della ref struct WarArena.Accessor

        /// <summary>
        ///     Trova la prima mossa legale per un serpente, per una simulazione base.
        /// </summary>
        private MoveDirection GetSimpleMove(ref WarSnake snake)
        {
            if (snake.Dead) return MoveDirection.Up; // Indifferente

            // Prova le 4 direzioni
            for (var i = 0; i < 4; i++)
            {
                var direction = (MoveDirection)i;
                var newHeadPos = _arena._field->GetNeighbor(snake.AsBody().Head, direction);
                if (!_arena._field->IsOccupied(newHeadPos)) return direction; // Trovata una mossa sicura
            }

            return MoveDirection.Up; // Morte inevitabile, si va su per convenzione
        }
    }

    /// <summary>
    ///     TIPO ANNIDATO: Il wrapper per l'array di serpenti.
    /// </summary>
    public readonly ref struct WarSnakeArray(ref WarArena arena)
    {
        private readonly ref WarArena _arena = ref arena;

        public ref WarSnake this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                var snakePtr = (byte*)_arena._snakes + index * _arena._snakeStride;
                return ref Unsafe.AsRef<WarSnake>(snakePtr);
            }
        }

        public uint Length => _arena._snakeCount;

        /// <summary>
        ///     Restituisce un enumeratore per iterare sull'array di serpenti.
        /// </summary>
        public Enumerator GetEnumerator() => new(ref _arena);

        /// <summary>
        ///     Un enumeratore leggero che itera sui WarSnake usando lo stride.
        /// </summary>
        public ref struct Enumerator
        {
            private readonly ref WarArena _arena;
            private int _index;

            public Enumerator(ref WarArena arena)
            {
                _arena = ref arena;
                _index = -1;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                _index++;
                return _index < _arena._snakeCount;
            }

            public ref WarSnake Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    var snakePtr = (byte*)_arena._snakes + _index * _arena._snakeStride;
                    return ref Unsafe.AsRef<WarSnake>(snakePtr);
                }
            }
        }
    }
}