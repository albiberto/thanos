// File: BattleSnake.cs

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Thanos.Enums;

namespace Thanos;

/// <summary>
/// Represents a single snake entity with a cache-optimized memory layout.
/// </summary>
/// <remarks>
/// --- CACHE-OPTIMIZED MEMORY LAYOUT (x64) ---
/// The struct is explicitly laid out to ensure optimal cache performance.
///
/// --- CACHE LINE 1: HEADER (0-63 bytes) ---
/// - Offset  0-3:  Health      (4 bytes)
/// - Offset  4-7:  Length      (4 bytes)
/// - Offset  8-11: MaxLength   (4 bytes)
/// - Offset 12-13: Head        (2 bytes)
/// - Offset 14-63: _padding    (50 bytes) -> Aligns the body to a 64-byte boundary.
///
/// --- CACHE LINE 2+: BODY (64+ bytes) ---
/// - Offset 64+:   Body array (ushort, 2 bytes per segment).
///
/// This layout ensures that all frequently-accessed header data is loaded in a single
/// cache line, and that sequential access to the body array is highly efficient.
/// </remarks>
[StructLayout(LayoutKind.Sequential, Pack = 64)]
public unsafe struct BattleSnake
{
    public const int HeaderSize = 64;

    // === CACHE LINE 1 - HEADER ===
    public int Health;
    public int Length;
    public int MaxLength;
    public ushort Head;
    private fixed byte _padding[HeaderSize - (sizeof(int) * 3 + sizeof(ushort))];
    
    // === CACHE LINE 2+ - BODY ARRAY ===
    // The fixed array starts at offset 64 (a new cache line).
    // The actual size is determined by the memory allocated in Battlefield.
    public fixed ushort Body[1];

    /// <summary>
    /// Processes a snake's move, updating its state based on the destination cell's content.
    /// </summary>
    /// <param name="bodyPtr">A pre-calculated pointer to the start of the body array.</param>
    /// <param name="newHeadPosition">The target board index for the head.</param>
    /// <param name="content">The content of the destination cell (from CollisionMatrix).</param>
    /// <param name="hazardDamage">The damage dealt by a hazard cell.</param>
    /// <returns>True if the snake survives the move; otherwise, false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Move(ushort* bodyPtr, ushort newHeadPosition, byte content, int hazardDamage)
    {
        // --- Phase 1: Update Health based on Destination ---
        // This logic handles all direct consequences of the move.
        // It's structured as if/else if to handle the enemy range check efficiently.

        switch (content)
        {
            // The content is 'Me' or another snake. This is a collision.
            case >= Constants.Me and <= Constants.Enemy8:
                Health = 0;
                return false; // Early exit on death to save memory operations.
            case Constants.Food:
                Health = 100; // Restore health to full.
                break;
            case Constants.Hazard:
                Health -= hazardDamage; // Apply hazard damage.
                break;
            // content is Empty (or an unhandled value)
            default:
                Health -= 1; // Standard health decay per turn.
                break;
        }

        // --- Phase 2: Post-Movement Death Check ---
        // Check if damage or decay has killed the snake.
        if (Health <= 0) return false; // Dead. Skip body update.

        // --- Phase 3: Update Body Position (only if alive) ---
        var hasEaten = content == Constants.Food;
        var canGrow = hasEaten && Length < MaxLength;
        var oldHead = Head;

        Head = newHeadPosition;

        if (canGrow)
        {
            // Growth Path: Append the old head to the body without removing the tail.
            Body[Length] = oldHead;
            Length++;
        }
        else
        {
            // Movement Path: Shift all body segments forward.
            if (Length > 1)
            {
                // This block copy is highly optimized for sequential memory access.
                Unsafe.CopyBlock(bodyPtr, bodyPtr + 1, (uint)(Length - 1) * sizeof(ushort));
            }
            if (Length > 0)
            {
                // The old head becomes the new tail segment.
                Body[Length - 1] = oldHead;
            }
        }

        return true; // Survived.
    }
}