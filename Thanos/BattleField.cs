using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Thanos.Enums;

namespace Thanos;

/// <summary>
/// A high-performance, minimalist grid representing the raw state of the board.
/// It manages an unmanaged, aligned memory buffer for maximum performance,
/// with padding for potential SIMD (Single Instruction, Multiple Data) operations.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct BattleField : IDisposable
{
    public const int Size = sizeof(long) + sizeof(uint);
    
    /// <summary>
    /// Pointer to the unmanaged memory block for the grid.
    /// </summary>
    private byte* _grid;
    
    /// <summary>
    /// The logical size of the board (width * height).
    /// </summary>
    private uint _boardSize;
    
    /// <summary>
    /// Gets the content of a cell at a specific grid index.
    /// </summary>
    public byte this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _grid[index];
    }
    
    /// <summary>
    /// Initializes the BattleField by allocating an aligned memory buffer.
    /// </summary>
    /// <param name="boardSize">The logical size of the board (width * height).</param>
    public void Initialize(uint boardSize)
    {
        _boardSize = boardSize;
        
        // Pad the physical memory size to be a multiple of the SIMD vector size.
        // This optimizes memory access for vectorized operations.
        var vectorSize = (uint)Vector<byte>.Count;
        var totalAllocatedMemory = (nuint)((boardSize + vectorSize - 1) / vectorSize * vectorSize);

        _grid = (byte*)NativeMemory.AlignedAlloc(totalAllocatedMemory, Constants.CacheLineSize);
    }

    /// <summary>
    /// Projects the current state of all active snakes onto the grid.
    /// </summary>
    /// <param name="tesla">A pointer to the main Tesla engine struct.</param>
    public void ProjectBattlefield(Tesla* tesla)
    {
        // Loop only over the snakes active in the current game.
        for (byte i = 0; i < tesla->ActiveSnakes; i++)
        {
            var snake = tesla->GetSnake(i);
            if (snake->Health <= 0) continue;

            var snakeId = (byte)(i + 1);
            
            // Project the head first.
            _grid[snake->Head] = snakeId;

            // Project the rest of the body by iterating through the circular buffer.
            var bodyIndex = snake->TailIndex;
            for (var j = 0; j < snake->Length - 1; j++)
            {
                var bodyPos = snake->Body[bodyIndex];
                _grid[bodyPos] = snakeId;
                
                // --- BUG CORRECTION ---
                // Use the bitwise AND with the pre-calculated mask for performance,
                // matching the optimization in the BattleSnake.Move method.
                bodyIndex = (bodyIndex + 1) & snake->CapacityMask;
            }
        }
    }

    /// <summary>
    /// Applies hazard positions to the grid.
    /// </summary>
    public void ApplyHazards(ReadOnlySpan<ushort> hazardPositions)
    {
        foreach (var position in hazardPositions) _grid[position] = Constants.Hazard;
    }
    
    /// <summary>
    /// Applies food positions to the grid.
    /// </summary>
    public void ApplyFood(ReadOnlySpan<ushort> foodPositions)
    {
        foreach (var position in foodPositions) _grid[position] = Constants.Food;
    }
 
    /// <summary>
    /// Clears the grid by setting all bytes to zero, preparing it for the next turn.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear() => Unsafe.InitBlock(_grid, Constants.Empty, _boardSize);

    /// <summary>
    /// Disposes of the struct by freeing its unmanaged memory.
    /// </summary>
    public void Dispose()
    {
        if (_grid == null) return;
        
        NativeMemory.AlignedFree(_grid);
        _grid = null;
    }
}