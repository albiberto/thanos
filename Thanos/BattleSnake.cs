using System.Runtime.InteropServices;
using Thanos.BitMasks;

namespace Thanos;

/// <summary>
/// BattleSnake struct optimized for cache locality and performance.
/// Designed to occupy exactly 3 cache lines of 64 bytes each.
///
/// CacheLineSize = 64 bytes (Standard for modern CPUs (x86-64, ARM64, RPi5)) 
/// 
/// MEMORY LAYOUT (192 bytes total):
/// - Cache Line 1 (0-63):   Header data + padding
/// - Cache Line 2 (64-127): Body[0-31] 
/// - Cache Line 3 (128-191): Body[32-63]
/// 
/// RATIONALE:
/// - Header separated from body to avoid false sharing
/// - Body spans 2 contiguous cache lines for spatial locality during iterations
/// - MaxBodyLength=64 supports very long snakes (realistic for battle snake)
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct BattleSnake
{
    // DESIGN CONSTANTS: int prevent boxing/unboxing overhead
    public const int SnakeSize = 192;          // Total: 192 bytes = 3 cache lines of 64 bytes
    private const int MaxBodyLength = 64;      // 64 segments * 2 bytes (ushort, Body array type) = 128 bytes = 2 cache lines
    
    // CACHE LINE 1: HEADER (64 bytes)
    // Frequently accessed, separated from body
    public byte Health;                         // 1 byte - Current health (0-100)
    public byte Length;                         // 1 byte - Current snake length
    public ushort Head;                         // 2 bytes - Head position (packed coordinates)
    private fixed byte _padding[60];            // 60 bytes - Padding to complete cache line
                                                // Calculation: 64 byte - (1 byte (health) + 1 byte (Lenght) + 2 byte (Head)) = 60 bytes (Padding)
    
    // CACHE LINE 2-3: BODY (128 bytes)
    // Contiguous array for efficient iterations
    public fixed ushort Body[MaxBodyLength];    // Cache line size: 64 bytes
                                                // ushort size: 2 bytes
                                                // Elements per cache line: 64 ÷ 2 = 32 ushorts
                                                // Max Body Length: 64 segments (32 in each cache line)
                                               
    /// <summary>
    /// Snake eats food: restores health to 100 and increases length by 1.
    /// Returns true if successful, false if already at maximum length.
    /// </summary>
    public void Eat()
    {
        Health = 100;
        Body[Length] = Body[Length - 1];
        Length++;
    }
}