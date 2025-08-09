using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Thanos.Enums;

namespace Thanos.War;

/// <summary>
///     A high-performance, minimalist grid representing the raw state of the board.
///     It manages an unmanaged, aligned memory buffer for maximum performance,
///     with padding for potential SIMD (Single Instruction, Multiple Data) operations.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct WarField : IDisposable
{
    public const uint Offset = sizeof(byte);

    // ======================================================================
    // === NO CACHE LINE MANAGMENT: 
    // The purpose of cache line alignment is to ensure that an entire data structure fits within a single cache line, avoiding multiple memory accesses.
    // When BattleArena allocates memory for BattleField, it is BattleArena itself that is responsible for ensuring that the start of that allocation is properly aligned.
    // Adding internal padding to the BattleField only wastes space because alignment is already handled at a higher level.
    // ======================================================================

    /// <summary>
    ///     Pointer to the unmanaged memory block for the grid.
    /// </summary>
    private byte* _grid;
    

    // ======================================================================
    // === END CACHE LINES
    // ======================================================================

    /// <summary>
    ///     Gets the content of a cell at a specific grid index.
    /// </summary>
    public byte this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _grid[index];
    }

    /// <summary>
    ///     Initializes the BattleField at a given memory location, using a pre-allocated grid buffer.
    /// </summary>
    /// <param name="battleField">Pointer to the memory for the BattleField struct.</param>
    /// <param name="gridMemory">Pointer to the pre-allocated memory for the grid.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void PlacementNew(WarField* battleField, byte* gridMemory) => battleField->_grid = gridMemory;

    /// <summary>
    ///     Projects the current state of all active snakes onto the grid.
    /// </summary>
    /// <param name="arena">A pointer to the main Tesla engine struct.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ProjectBattleField(WarArena* arena)
    {
        // Loop only over the snakes active in the current game.
        for (byte i = 0; i < arena->SnakesCount; i++)
        {
            var snake = arena->GetSnake(i);
            // if (snake->Dead) continue;

            var snakeId = (byte)(i + 1);

            // Project the entire body including head
            // var bodyIndex = snake->TailIndex;
            // for (var j = 0; j < snake->Length - 1; j++) // Length - 1 because the head is separate
            // {
            //     var bodyPos = snake->Body[bodyIndex];
            //     _grid[bodyPos] = snakeId;
            //     bodyIndex = (bodyIndex + 1) & snake->Length; // Wrap around using bitwise AND
            // }
            //
            // // Project the head separately
            // _grid[snake->Head] = snakeId;
        }
    }

    /// <summary>
    ///     Applies hazard positions to the grid.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ApplyHazards(ushort* hazards, int count)
    {
        for (var i = 0; i < count; i++) _grid[hazards[i]] = Constants.Hazard;
    }

    /// <summary>
    ///     Applies food positions to the grid.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ApplyFoods(ushort* foods, int count)
    {
        for (var i = 0; i < count; i++) _grid[foods[i]] = Constants.Hazard;
    }

    /// <summary>
    ///     Applies a batch of turn updates to the battlefield grid.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Update(TurnUpdate* updates, int count)
    {
        for (var i = 0; i < count; i++)
        {
            var update = updates + i;
            _grid[update->NewHead] = update->SnakeId;
            if (!update->HasEaten) _grid[update->OldTail] = Constants.Empty;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RemoveSnake(WarSnake* snake)
    {
        var currentIndex = snake->TailIndex;

        for (var i = 0; i < snake->Length; i++)
        {
            var position = snake->Body[currentIndex];

            _grid[position] = Constants.Empty;

            currentIndex = (currentIndex + 1) & snake->Length;
        }
    }

    /// <summary>
    ///     Disposes of the struct by freeing its unmanaged memory.
    /// </summary>
    public void Dispose() => NativeMemory.AlignedFree(_grid);

    /// <summary>
    ///     A struct to hold all necessary data for a single snake's grid update.
    /// </summary>
    public readonly struct TurnUpdate(ushort newHead, int oldTail, byte snakeId, bool hasEaten)
    {
        public const int TurnSize = sizeof(ushort) * 2 + sizeof(byte) + sizeof(bool);

        public readonly ushort NewHead = newHead;
        public readonly int OldTail = oldTail;
        public readonly byte SnakeId = (byte)(snakeId + 1);
        public readonly bool HasEaten = hasEaten;
    }
}