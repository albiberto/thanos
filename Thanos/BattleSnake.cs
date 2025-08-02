// File: BattleSnake.cs

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Thanos.BitMasks;

namespace Thanos;

/// <summary>
/// BattleSnake represents a single snake entity with cache-optimized memory layout.
/// The structure is designed to minimize cache misses during game logic execution.
/// </summary>
/// <remarks>
/// CACHE LINE ORGANIZATION:
/// 
/// The structure is carefully laid out to ensure optimal cache performance:
/// - Header data (frequently accessed together) occupies exactly one cache line
/// - Body array starts at cache line boundary for efficient sequential access
/// 
/// MEMORY LAYOUT (64-bit systems):
/// 
/// CACHE LINE 1 - HEADER (0-63 bytes):
/// - Offset  0-3:  Health    (4 bytes) - Current health points
/// - Offset  4-7:  Length    (4 bytes) - Current snake length
/// - Offset  8-11: MaxLength (4 bytes) - Maximum allowed length
/// - Offset 12-13: Head      (2 bytes) - Current head position
/// - Offset 14-63: _padding  (50 bytes) - Explicit padding to align body to cache line
/// 
/// CACHE LINE 2+ - BODY (64+ bytes):
/// - Offset 64+: Body array (2 bytes per segment)
/// - Each cache line holds 32 body segments (64 bytes / 2 bytes per ushort)
/// 
/// This layout ensures:
/// 1. All header fields are in the same cache line for atomic access
/// 2. Body array starts on a new cache line boundary
/// 3. No false sharing between header and body data
/// 4. Sequential body access is cache-friendly
/// </remarks>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct BattleSnake
{
    public const int HeaderSize = 64; // Exactly one cache line

    // === CACHE LINE 1 - HEADER (64 bytes) ===
    
    // Core snake state - accessed together during game logic
    public int Health;      // 4 bytes - Snake's current health (0-100)
    public int Length;      // 4 bytes - Current number of body segments
    public int MaxLength;   // 4 bytes - Maximum allowed body segments
    public ushort Head;     // 2 bytes - Current head position on the board
    
    // Explicit padding to ensure body array starts at cache line boundary
    // This prevents false sharing and ensures optimal memory access patterns
    private fixed byte _padding[HeaderSize - (sizeof(int) * 3 + sizeof(ushort))]; // 50 bytes
    
    // === CACHE LINE 2+ - BODY ARRAY ===
    
    // Body segments array - starts at offset 64 (new cache line)
    // Using fixed array syntax for direct memory access without indirection
    public fixed ushort Body[1]; // Actual size determined by MaxLength

    /// <summary>
    /// Processes a snake movement, updating state based on destination content.
    /// Optimized for minimal cache misses by accessing data in predictable patterns.
    /// </summary>
    /// <param name="bodyPtr">Precalculated pointer to body array for efficiency</param>
    /// <param name="newHeadPosition">Target position for the head</param>
    /// <param name="content">Content of the destination cell</param>
    /// <param name="hazardDamage">Damage dealt by hazard cells</param>
    /// <returns>True if snake survives the move, false if dead</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Move(ushort* bodyPtr, ushort newHeadPosition, CellContent content, int hazardDamage = 15)
    {
        // --- Phase 1: State Update Based on Destination ---
        // Handle primary cases that determine life, death, or recovery.
        // This switch is optimized for branch prediction with most common cases first.
        switch (content)
        {
            case CellContent.EnemySnake:
                // Instant death conditions - no need to update body
                Health = 0;
                return false; // Early exit saves unnecessary memory operations

            case CellContent.Food:
                Health = 100; // Full health restoration
                break; // Continue to handle growth

            case CellContent.Hazard:
                Health -= hazardDamage; // Apply hazard damage
                break;

            case CellContent.Empty:
            default:
                Health -= 1; // Standard movement cost
                break;
        }

        // --- Phase 2: Post-Movement Death Check ---
        // Check if damage has killed the snake
        if (Health <= 0)
        {
            return false; // Dead - skip body update to save memory operations
        }

        // --- Phase 3: Body Update (only if alive) ---
        // This section touches memory, so we only execute it for living snakes
        var hasEaten = content == CellContent.Food;
        var canGrow = hasEaten && Length < MaxLength;
        ushort oldHead = Head;

        // Update head position
        Head = newHeadPosition;

        if (canGrow)
        {
            // GROWTH PATH: Add old head to body without removing tail
            // This is a simple append operation - cache friendly
            Body[Length] = oldHead;
            Length++;
        }
        else
        {
            // MOVEMENT PATH: Shift body segments forward
            // This memory copy is optimized for sequential access
            if (Length > 1)
            {
                // Shift all body segments one position forward
                // Uses optimized block copy for better cache utilization
                Unsafe.CopyBlock(bodyPtr, bodyPtr + 1, (uint)(Length - 1) * sizeof(ushort));
            }
            if (Length > 0)
            {
                // Place old head at the end of the body
                Body[Length - 1] = oldHead;
            }
        }

        return true; // Snake survived
    }

    /// <summary>
    /// Resets the snake to initial state.
    /// Only touches header cache line, body memory is already zeroed by Battlefield.
    /// </summary>
    /// <param name="maxLength">Maximum allowed body length</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Reset(int maxLength)
    {
        // Initialize header fields - all in the same cache line
        Health = 100;              // Full health
        Length = 3;                // Standard starting length
        MaxLength = maxLength;     // Set maximum growth limit
        Head = 0;                  // Dummy initial position
        
        // Body array doesn't need explicit zeroing because:
        // 1. Battlefield already zeroed all memory with Unsafe.InitBlock
        // 2. Length field defines the valid portion of the body array
        // This saves unnecessary memory writes and cache pollution
    }
}