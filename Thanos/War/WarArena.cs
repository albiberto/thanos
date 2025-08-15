using System;
using Thanos.Memory;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Thanos.War;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WarArena
{
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
                var snakePtr = (byte*)_arena._snakes + (index * _arena._snakeStride);
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

public static class WarArenaExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WarArena.WarSnakeArray Snakes(this ref WarArena arena) => new(ref arena);
}