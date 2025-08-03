using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Thanos;

/// <summary>
/// Represents a high-performance, cache-friendly, and SIMD-optimized game board for collision detection.
/// Manages its own aligned, unmanaged memory buffer.
/// </summary>
public unsafe struct CollisionMatrix : IDisposable
{
    private byte* _grid;        // Puntatore alla memoria non gestita che rappresenta la griglia.
    private int _boardSize;     // Dimensione logica della griglia (width * height).
    private nuint _totalMemory; // Dimensione fisica della memoria allocata, con padding per SIMD.
    
    /// <summary>
    /// Gets the content of a cell at a specific board coordinate (index).
    /// </summary>
    public byte this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _grid[index];
    }
    
    /// <summary>
    /// Initializes the GameBoard, allocating aligned memory.
    /// Should be called once at the start of the game.
    /// </summary>
    /// <param name="width">The width of the game board.</param>
    /// <param name="height">The height of the game board.</param>
    public void Initialize(int width, int height)
    {
        _boardSize = width * height;

        // For SIMD operations, the memory size must be a multiple of the vector size.
        // We use Vector256 (32 bytes), so we pad the memory allocation to the next multiple of 32.
        var vectorSize = Vector<byte>.Count;
        _totalMemory = (nuint)((_boardSize + vectorSize - 1) / vectorSize * vectorSize);

        // Allocate memory aligned to a 64-byte boundary for optimal cache and SIMD performance.
        _grid = (byte*)NativeMemory.AlignedAlloc(_totalMemory, 64);
        if (_grid == null)
            throw new OutOfMemoryException($"Failed to allocate {_totalMemory} bytes for GameBoard");
    }

    /// <summary>
    /// Clears the entire game board to zero using SIMD instructions for maximum speed.
    /// This is the equivalent of a highly optimized memset.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        if (_grid == null) return;

        var zeroVector = Vector<byte>.Zero;
        // Converte la dimensione del vettore in nuint una sola volta per efficienza
        var vectorSize = (nuint)Vector<byte>.Count;

        // Usa 'nuint' per la variabile del ciclo 'i' per renderla compatibile con '_totalMemory'
        for (nuint i = 0; i < _totalMemory; i += vectorSize) Unsafe.WriteUnaligned(_grid + i, zeroVector);
    }

    /// <summary>
    /// Projects the current state of all snakes from a Battlefield onto this grid.
    /// </summary>
    /// <param name="battlefield">A pointer to the Battlefield containing the snake data.</param>
    /// <param name="maxSnakes">The maximum number of snakes to process.</param>
    public void ProjectBattlefield(Battlefield* battlefield, int maxSnakes)
    {
        // Iterate over all possible snake slots.
        for (var i = 0; i < maxSnakes; i++)
        {
            var snake = battlefield->GetSnake(i);
            if (snake == null || snake->Health <= 0) continue;

            // Use the snake's index + 1 as its ID on the board (0 is reserved for 'Empty').
            var snakeId = (byte)(i + 1);

            // "Paint" the snake's body onto the grid.
            var bodyPtr = (ushort*)((byte*)snake + BattleSnake.HeaderSize);
            for (int j = 0; j < snake->Length; j++) _grid[bodyPtr[j]] = snakeId;
            
            // "Paint" the head over any body part that might have been there.
            _grid[snake->Head] = snakeId;
        }
    }
    
    /// <summary>
    /// Frees the unmanaged memory used by the grid.
    /// Must be called when the GameBoard is no longer needed.
    /// </summary>
    public void Dispose()
    {
        if (_grid == null) return;
        
        NativeMemory.AlignedFree(_grid);
        _grid = null;
        _boardSize = 0;
        _totalMemory = 0;
    }
}