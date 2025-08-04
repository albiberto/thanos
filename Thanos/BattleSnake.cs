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
    public ushort Head;
    private fixed byte _padding[HeaderSize - (sizeof(int) * 2 + sizeof(ushort))];
    
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
        // A flag to pass the result of the food check from Phase 1 to Phase 3
        // without re-evaluating the 'content' variable.
        var hasEaten = false;

        // --- Phase 1: Update Health based on Destination ---
        switch (content)
        {
            case >= Constants.Me and <= Constants.Enemy8:
                Health = 0;
                return false;

            case Constants.Food:
                Health = 100;
                hasEaten = true; // Set the flag here
                break;

            case Constants.Hazard:
                Health -= hazardDamage;
                break;
        
            default: // Assumed to be Empty
                Health -= 1;
                break;
        }

        // --- Phase 2: Post-Movement Death Check ---
        if (Health <= 0 || Length <= 0) return false;

        // --- Phase 3: Update Body Position (only if alive) ---
        // The 'canGrow' logic now uses the pre-calculated 'hasEaten' flag.
        // This avoids a second check on 'content', optimizing the instruction flow.
        var oldHead = Head;

        Head = newHeadPosition;

        if (hasEaten)
        {
            // Growth Path
            Body[Length] = oldHead;
            Length++;
        }
        else
        {
            // Movement Path
            Unsafe.CopyBlock(bodyPtr, bodyPtr + 1, (uint)(Length - 1) * sizeof(ushort));
            Body[Length - 1] = oldHead;
        }

        return true;
    }
}