using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Thanos;

/// <summary>
/// A high-performance, minimalist grid representing the raw state of the board.
/// It manages an unmanaged, aligned memory buffer for maximum performance.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 64)]
public unsafe struct CollisionMatrix : IDisposable
{
    // --- Struct Fields (Total 64 bytes, 1 cache line) ---
    private byte* _grid;                    // 8 bytes: Pointer to the state grid (snakes, hazards)
    private ushort _boardSize;              // 2 bytes: Logical size of the board (width * height)
    private fixed byte _manualPadding[56];  // 48 bytes: Fills the rest with the cache line
    
    private const byte EMPTY_ID = 0;      // Reserved value for empty cells
    private const byte FOOD_ID = HAZARD_ID / 2;     // Reserved value for foods
    private const byte HAZARD_ID = byte.MaxValue;   // Reserved value for hazards

    /// <summary>
    /// Gets the content of a cell at a specific grid coordinate (index).
    /// </summary>
    public byte this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _grid[index];
    }

    /// <summary>
    /// Initializes the CollisionMatrix by allocating the master memory buffer.
    /// </summary>
    /// <param name="boardSize">The logical size of the board (width * height).</param>
    public void Initialize(ushort boardSize)
    {
        _boardSize = boardSize;
        
        // Calculate the physical memory size, padded to be a multiple of the SIMD vector size.
        var vectorSize = Vector<byte>.Count;
        var totalAllocatedMemory = (nuint)((boardSize + vectorSize - 1) / vectorSize * vectorSize);

        // Allocate a 64-byte aligned memory block.
        _grid = (byte*)NativeMemory.AlignedAlloc(totalAllocatedMemory, 64);
    }

    /// <summary>
    /// Projects the current state of all snakes onto the grid.
    /// </summary>
    public void ProjectBattlefield(Battlefield* battlefield, int maxSnakes)
    {
        for (byte i = 0; i < maxSnakes; i++)
        {
            var snake = battlefield->GetSnake(i);
            if (snake->Health <= 0) continue;

            var snakeId = (byte)(i + 1);
            var bodyPtr = (ushort*)((byte*)snake + BattleSnake.HeaderSize);
            for (var j = 0; j < snake->Length; j++) _grid[bodyPtr[j]] = snakeId;
        }
    }

    /// <summary>
    /// Applies hazards to the grid by directly writing to the specified positions.
    /// This method handles any possible hazard layout.
    /// </summary>
    public void ApplyHazards(ReadOnlySpan<ushort> hazardPositions)
    {
        foreach (var position in hazardPositions) _grid[position] = HAZARD_ID;
    }
    
    /// <summary>
    /// Applica le posizioni del cibo alla griglia.
    /// </summary>
    public void ApplyFood(ReadOnlySpan<ushort> foodPositions)
    {
        foreach (var position in foodPositions) _grid[position] = FOOD_ID;
    }
 
    /// <summary>
    /// Clears the grid by setting all bytes to zero, preparing it for the next turn.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear() => Unsafe.InitBlock(_grid, EMPTY_ID, (uint)_grid);
    
    /// <summary>
    /// Disposes of the struct by freeing its unmanaged memory.
    /// </summary>
    public void Dispose() => NativeMemory.AlignedFree(_grid);
}