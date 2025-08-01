using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

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
    private byte _isInitialized;                // 1 byte - Pre-warm flag
    private fixed byte _padding[59];            // 59 bytes - Padding to complete cache line
    // Calculation: 64 byte - (1 + 1 + 2 + 1) = 59 bytes (Padding)
    
    // CACHE LINE 2-3: BODY (128 bytes)
    // Contiguous array for efficient iterations
    public fixed ushort Body[MaxBodyLength];    // Cache line size: 64 bytes
    // ushort size: 2 bytes
    // Elements per cache line: 64 ÷ 2 = 32 ushorts
    // Max Body Length: 64 segments (32 in each cache line)
    
    /// <summary>
    /// Pre-warms the snake by initializing it to a default state.
    /// This avoids runtime initialization overhead and ensures predictable memory patterns.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PreWarm()
    {
        if (_isInitialized != 0) return; // Already pre-warmed
        
        // Initialize to default snake state
        Health = 100;
        Length = 3;  // Standard starting length
        Head = 0;    // Will be set properly during game initialization
            
        // Pre-initialize body with bulk zero operation
        // This ensures all cache lines are touched and memory is committed
        Unsafe.InitBlock(ref Unsafe.As<ushort, byte>(ref Body[0]), 0, MaxBodyLength * sizeof(ushort));
        
        _isInitialized = 1;
    }
    
    /// <summary>
    /// Resets snake to initial state without deallocating memory
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Reset()
    {
        Health = 100;
        Length = 3;
        Head = 0;
        
        // Pre-initialize body with bulk zero operation
        // This ensures all cache lines are touched and memory is committed
        Unsafe.InitBlock(ref Unsafe.As<ushort, byte>(ref Body[0]), 0, MaxBodyLength * sizeof(ushort));
    }
    
    /// <summary>
    /// Snake eats food: restores health to 100 and increases length by 1.
    /// Returns true if successful, false if already at maximum length.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Eat()
    {
        if (Length >= MaxBodyLength) return false;
        
        Health = 100;
        Body[Length] = Body[Length - 1]; // Duplicate tail segment
        Length++;
        return true;
    }
}