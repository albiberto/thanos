using System;
using Thanos.Memory;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Thanos.MCST;

namespace Thanos.War;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WarArena
{
    /// <summary>
    /// L'UNICA porta d'accesso pubblica ai dati e alle operazioni dell'arena.
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
    /// TIPO ANNIDATO: Funge da API pubblica e sicura per la WarArena.
    /// Ora contiene anche la logica di gioco.
    /// </summary>
    public readonly unsafe ref struct Accessor(ref WarArena arena)
    {
        private readonly ref WarArena _arena = ref arena;

        /// <summary>
        /// Restituisce il wrapper per l'array di serpenti.
        /// CORREZIONE: Ora chiama il costruttore corretto di WarSnakeArray.
        /// </summary>
        public WarSnakeArray Snakes => new(ref _arena);

        /// <summary>
        /// Calcola le mosse legali per il nostro serpente (indice 0) e le scrive nello span fornito.
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
                if (!_arena._field->IsOccupied(newHeadPos))
                {
                    legalMoves[moveCount++] = direction;
                }
            }
            return moveCount;
        }

        /// <summary>
        /// Applica una mossa per il nostro serpente, aggiornando lo stato dell'arena.
        /// </summary>
        public void ApplyMove(MoveDirection move)
        {
            ref var me = ref Snakes[0];
            if (me.Dead) return;

            var body = me.AsBody();
            var oldTail = body.Tail; // Salva la vecchia coda prima di muovere
            var newHead = _arena._field->GetNeighbor(body.Head, move);
            
            // Controlli di base
            var hasEaten = _arena._field->IsFood(newHead);
            var hazardDamage = _arena._field->IsHazard(newHead) ? 15 : 0;

            // Muovi il nostro serpente
            me.Move(newHead, hasEaten, hazardDamage);
            
            // Aggiorna la mappa (bitboard)
            if (!me.Dead)
            {
                _arena._field->UpdateSnakePosition(oldTail, newHead, hasEaten);
            }

            // QUI ANDREBBE LA LOGICA PER MUOVERE GLI AVVERSARI E GESTIRE LE COLLISIONI
            // Per ora, ci concentriamo solo sul nostro movimento.
            
            // Controlla se siamo morti dopo la mossa
            if (me.Dead)
            {
                _arena._liveSnakesCount--;
                // Rimuovi il serpente morto dalla mappa
                // _arena._field->RemoveSnake(...);
            }
        }
        
        /// <summary>
        /// Valuta lo stato attuale del gioco.
        /// </summary>
        /// <returns>1.0 per vittoria, -1.0 per sconfitta, 0.0 se il gioco continua.</returns>
        public float Evaluate()
        {
            ref var me = ref Snakes[0];

            if (me.Dead)
            {
                return -1.0f; // Sconfitta
            }

            if (_arena._liveSnakesCount <= 1)
            {
                return 1.0f; // Vittoria (siamo gli unici rimasti)
            }
            
            return 0.0f; // Gioco in corso
        }
    }

    /// <summary>
    /// TIPO ANNIDATO: Il wrapper per l'array di serpenti.
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
        /// Restituisce un enumeratore per iterare sull'array di serpenti.
        /// </summary>
        public Enumerator GetEnumerator() => new(ref _arena);

        /// <summary>
        /// Un enumeratore leggero che itera sui WarSnake usando lo stride.
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
                    var snakePtr = (byte*)_arena._snakes + (_index * _arena._snakeStride);
                    return ref Unsafe.AsRef<WarSnake>(snakePtr);
                }
            }
        }
    }
}