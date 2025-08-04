using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Thanos.Enums;

namespace Thanos;

/// <summary>
/// Represents a single snake entity with a cache-optimized memory layout.
/// The snake's body is managed as a circular buffer for O(1) move operations.
/// </summary>
/// <remarks>
/// --- CACHE-OPTIMIZED MEMORY LAYOUT (x64) ---
/// The struct is explicitly laid out to align with CPU cache lines (64 bytes),
/// minimizing memory access latency. This is a form of Data-Oriented Design.
///
/// --- CACHE LINE 1: Header (0-63 bytes) ---
/// Contains all frequently-accessed state data. By grouping this data into a single
/// 64-byte block, a single CPU cache-fetch can load all the information needed
/// for state checks and move logic, drastically improving performance.
///
/// - Health, Length: Core game state.
/// - Head: The absolute board position of the snake's head for quick lookups.
/// - CapacityMask, HeadIndex, TailIndex: Fields to manage the circular buffer logic.
/// - _padding: Ensures the header perfectly fills the first cache line, so the
///   body array starts on a new cache line boundary.
///
/// --- CACHE LINE 2+: Body (64+ bytes) ---
/// The snake's body is stored in a circular buffer starting at a 64-byte offset.
/// This layout prevents the larger body array from polluting the cache line
/// that holds the critical header state. The actual size of this buffer is
/// determined at runtime by the 'Tesla' struct and must be a power of two
/// for bitwise optimizations to work.
/// </remarks>
[StructLayout(LayoutKind.Sequential, Pack = 64)]
public unsafe struct BattleSnake
{
    private const int PaddingCount = 1;
    private const int PaddingSize = Constants.CacheLineSize - sizeof(int) * 5 - sizeof(ushort) * 1;
    
    public const int HeaderSize = PaddingCount * Constants.CacheLineSize;

    // === CACHE LINE 1 - HEADER ===
    public int Health;
    public int Length;
    
    /// <summary>
    /// The bitmask for circular buffer operations, pre-calculated as (capacity - 1).
    /// </summary>
    public int CapacityMask;

    public int HeadIndex;
    public int TailIndex;
    public ushort Head;
    
    // Padding to fill the 64-byte cache line
    private fixed byte _padding[PaddingSize];

    // === CACHE LINE 2+ - BODY ARRAY (Circular Buffer) ===
    public fixed ushort Body[1];

    /// <summary>
    /// Resets the snake to a default state at a specific position.
    /// The provided capacity must be a power of two for move optimizations to work correctly.
    /// </summary>
    /// <param name="head">The starting board position for the snake's head.</param>
    /// <param name="capacityMask">The physical size of the allocated body buffer. Must be a power of two (e.g., 128).</param>
    public void Reset(ushort head, int capacityMask)
    {
        Health = 100;
        Length = 1;
        Head = head;
        CapacityMask = capacityMask - 1; // Pre-calculate and store the bitmask to avoid a subtraction in the hot path (Move method). 
        HeadIndex = 0;
        TailIndex = 0;
        Body[0] = head;
    }

    /// <summary>
    /// Processes a single turn for the snake, updating its state based on the move's outcome.
    /// Uses a highly efficient circular buffer with bitwise operations for body management.
    /// </summary>
    /// <returns>True if the snake survives the move; otherwise, false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Move(ushort newHeadPosition, byte content, int hazardDamage)
    {
        var hasEaten = false;

        switch (content)
        {
            case >= Constants.Me and <= Constants.Enemy7: Health = 0; return false;
            case Constants.Food: Health = 100; hasEaten = true; break;
            case Constants.Hazard: Health -= hazardDamage; break;
            default: Health -= 1; break;
        }

        if (Health <= 0) return false;

        var oldHead = Head;
        Head = newHeadPosition;

        // Use the pre-calculated mask for a fast, branchless wrap-around.
        // This is equivalent to (HeadIndex + 1) % capacity but significantly faster.
        var nextHeadIndex = (HeadIndex + 1) & CapacityMask;

        if (hasEaten)
        {
            Length++;
        }
        else
        {
            // If the snake doesn't eat, its tail also moves forward.
            TailIndex = (TailIndex + 1) & CapacityMask;
        }
        
        // The previous head's position becomes the new body segment at the head of the buffer.
        Body[nextHeadIndex] = oldHead;
        HeadIndex = nextHeadIndex;

        return true;
    }
}