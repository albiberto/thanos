using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Thanos.Enums;

namespace Thanos;

/// <summary>
/// Represents a single snake entity with a cache-optimized memory layout.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 64)]
public unsafe struct BattleSnake
{
    // --- Constants for memory layout ---
    private const int HeaderFieldCount = 7; // Health, Length, Capacity, HeadIndex, TailIndex, Head, Tail
    private const int PaddingSize = Constants.CacheLineSize - sizeof(int) * 5 - sizeof(ushort) * 1;
    public const int HeaderSize = Constants.CacheLineSize;

    // ===================================
    // === CACHE LINE 1: HEADER (64 bytes)
    // ===================================
    // All critical and frequently-accessed data resides here.
    
    private int _capacity; // The maximum capacity of the Body buffer.
    private int _nextHeadIndex; // The index where the next head position will be written.
    
    public int Health { get; private set; } // The current health of the snake, which can be reduced by damage or reset to 100 when eating.
    public int Length { get; private set; } // The current length of the snake, which is the number of segments in its body.
    
    public ushort Head { get; private set; } // The current position of the snake's head on the board.
    public int TailIndex { get; private set; } // The index of the current tail position in the Body array.

    // Padding to fill the cache line and align the Body array
    private fixed byte _padding[PaddingSize];

    // ========================================
    // === CACHE LINE 2+: BODY (Circular Buffer)
    // ========================================
    public fixed ushort Body[1];


    /// <summary>
    /// Reconstructs a snake from an existing game state at a specific memory address.
    /// Used for deserializing a game turn.
    /// </summary>
    /// <param name="snake">Pointer to the memory to initialize.</param>
    /// <param name="health">The snake's current health.</param>
    /// <param name="length">The snake's current length.</param>
    /// <param name="capacity">The capacity of the body buffer.</param>
    /// <param name="sourceBody">A span containing the snake's body coordinates, from head to tail.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void PlacementNew(BattleSnake* snake, int health, int length, int capacity, ReadOnlySpan<ushort> sourceBody)
    {
        snake->Health = health;
        snake->Length = length;
        snake->_capacity = capacity;

        snake->Head = sourceBody[0];
    
        var destinationBody = new Span<ushort>(snake->Body, capacity);
        sourceBody.CopyTo(destinationBody);
        
        snake->_nextHeadIndex = length % capacity;
        snake->TailIndex = length - 1;
    }

    /// <summary>
    /// Processes a single turn for the snake, updating its state.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Move(ushort newHeadPosition, bool hasEaten, int damage = 0)
    {
        // If the snake is dead, no further processing is needed.
        // But for performance reasons, we check in the caller.
        
        // 1. Update health
        if (hasEaten)
        {
            Health = 100;
        }
        else
        {
            Health -= damage + 1;
        }

        // If the move is fatal, set state to dead and exit
        if (Dead) return;
        
        // The bitwise mask for circular buffer operations, derived from Capacity
        var capacityMask = _capacity - 1;

        // 2. Update body and head position
        Body[_nextHeadIndex] = Head;
        Head = newHeadPosition;
        _nextHeadIndex = (_nextHeadIndex + 1) & capacityMask;

        // 3. Handle length and tail movement
        if (hasEaten && Length < _capacity)
        {
            // Case 1: The snake eats and has room to grow.
            Length++;
        }
        else
        {
            // Case 2: The snake moves without eating.
            TailIndex = (TailIndex + 1) & capacityMask;
        }
    }

    /// <summary>
    /// Indicates if the snake is dead (health is zero or less).
    /// </summary>
    public readonly bool Dead
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Health <= 0;
    }

    /// <summary>
    /// Sets the snake's health to zero.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Kill() => Health = 0;
}