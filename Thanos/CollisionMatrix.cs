using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Thanos;

/// <summary>
/// Represents a high-performance, cache and SIMD-optimized game grid for collision detection.
/// It manages its own unmanaged and aligned memory buffer.
/// </summary>
public unsafe struct CollisionMatrix : IDisposable
{
    private byte* _grid;        // 8 bytes on x64 systems (pointer)
    private int _boardSize;     // 4 bytes (32-bit integer)
    private nuint _totalMemory; // 8 bytes on x64 systems (native-sized integer)

    // --- Struct Size Analysis on x64 Systems ---
    // _grid:        8 bytes
    // _boardSize:   4 bytes
    // (padding):    4 bytes (inserted by the compiler to align the next field)
    // _totalMemory: 8 bytes
    // --------------------
    // Total:        24 bytes

    /// <summary>
    /// Gets the content of a cell at a specific grid coordinate (index).
    /// </summary>
    public byte this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _grid[index];
    }

    /// <summary>
    /// Initializes the CollisionMatrix by allocating aligned memory.
    /// This should be called once at the start of the game.
    /// </summary>
    /// <param name="boardSize">The size of the board (width * height).</param>
    public void Initialize(int boardSize)
    {
        _boardSize = boardSize;

        var vectorSize = Vector<byte>.Count;
        _totalMemory = (nuint)((_boardSize + vectorSize - 1) / vectorSize * vectorSize);

        _grid = (byte*)NativeMemory.AlignedAlloc(_totalMemory, 64);
        if (_grid == null)
            throw new OutOfMemoryException($"Failed to allocate {_totalMemory} bytes for the CollisionMatrix");
    }

    /// <summary>
    /// Clears the entire game grid using the most efficient intrinsic method.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        if (_grid == null) return;
        Unsafe.InitBlock(_grid, 0, (uint)_totalMemory);
    }

    /// <summary>
    /// Projects the current state of all snakes from a Battlefield onto this grid.
    /// </summary>
    /// <param name="battlefield">A pointer to the Battlefield containing the snake data.</param>
    /// <param name="maxSnakes">The maximum number of snakes to process.</param>
    public void ProjectBattlefield(Battlefield* battlefield, int maxSnakes)
    {
        for (var i = 0; i < maxSnakes; i++)
        {
            var snake = battlefield->GetSnake(i);
            if (snake == null || snake->Health <= 0) continue;

            var snakeId = (byte)(i + 1);
            var bodyPtr = (ushort*)((byte*)snake + BattleSnake.HeaderSize);
            
            for (int j = 0; j < snake->Length; j++)
            {
                _grid[bodyPtr[j]] = snakeId;
            }
            _grid[snake->Head] = snakeId;
        }
    }

    /// <summary>
    /// Frees the unmanaged memory used by the grid.
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