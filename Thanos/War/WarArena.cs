using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Thanos.War;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WarArena
{
    // --- DATI PRIVATI ---
    // Dettagli implementativi completamente incapsulati.
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
    /// TIPO ANNIDATO: Il wrapper per l'array di serpenti ora vive qui.
    /// Essendo annidato, ha accesso ai campi privati di WarArena.
    /// </summary>
    public readonly ref struct WarSnakeArray(ref WarArena arena)
    {
        private readonly ref WarArena _arena = ref arena;
        
        public ref WarSnake this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                // Calcola l'offset usando i campi privati dell'arena
                var snakePtr = (byte*)_arena._snakes + (index * _arena._snakeStride);
                return ref Unsafe.AsRef<WarSnake>(snakePtr);
            }
        }

        public uint Length => _arena._snakeCount;
    }
}

public static class WarArenaExtensions
{
    /// <summary>
    /// Metodo di estensione che crea un wrapper WarSnakeArray per una data WarArena.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WarArena.WarSnakeArray Snakes(this ref WarArena arena) => new(ref arena);
}