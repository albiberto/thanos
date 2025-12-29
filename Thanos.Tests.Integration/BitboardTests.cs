using Thanos.War.Structures;

namespace Thanos.Tests.Integration;

/// <summary>
///     Integration tests for the <see cref="Bitboard"/> structure.
///     Validates memory safety, SIMD operations, and logical bit manipulation
///     across various board sizes and alignment boundaries.
/// </summary>
[TestFixture]
[Parallelizable(ParallelScope.All)]
public partial class BitboardTests
{
    /// <summary>
    ///     Generates test cases covering a wide range of bitboard sizes, from sub-64 bits to 2048 bits.
    ///     This ensures coverage for all internal optimization paths: Scalar, Vector128, Vector256, and manual unrolling.
    /// </summary>
    public static IEnumerable<TestCaseData> TestDimensions
    {
        get
        {
            // Range: From tiny boards (2 bits) to massive ones (2048 bits / 256 bytes).
            // This guarantees we hit every switch-case in the SIMD logic.
            int[] logicalBitSizes = [2, 4, 8, 16, 32, 64, 96, 128, 192, 256, 320, 384, 448, 512, 1024, 2048];

            foreach (var size in logicalBitSizes)
            {
                // --- MEMORY ALIGNMENT STRATEGY ---
                // The BitBoard uses 'ulong*' (64-bit pointers) for high-performance access.
                // Therefore, the underlying byte buffer MUST be aligned to 8-byte boundaries.
                //
                // Formula: Round up to the nearest multiple of 64 bits, then convert to bytes.
                // Example: User needs 10 bits -> We allocate 64 bits (8 Bytes).
                //
                // [ 0.........9 ] [ 10............................................63 ]
                //   LOGICAL DATA    PADDING / SAFETY ZONE (Must remain untouched)
                
                var bufferSizeBytes = (size + 63) / 64 * 8;
                
                var physicalBits = (ushort)(bufferSizeBytes * 8); 
                var physicalMaxIndex = (ushort)(physicalBits - 1);

                // CASE 1: Physical Boundary (Stress Test)
                // Tests operations up to the absolute limit of allocated memory.
                // In this scenario, Logical == Physical, so there is no padding zone.
                yield return new TestCaseData(physicalMaxIndex, bufferSizeBytes)
                    .SetName($"Size_{size}b_PhysicalMax");

                // CASE 2: Logical Boundary (Real World Usage)
                // Tests operations strictly within the requested game logic size.
                // Crucial for verifying that the 'Safety Zone' (padding) remains uncorrupted.
                var logicalMaxIndex = (ushort)(size - 1);
                yield return new TestCaseData(logicalMaxIndex, bufferSizeBytes)
                    .SetName($"Size_{size}b_LogicalMax");

                // CASE 3: Half Capacity
                // Routine sanity check to ensure no off-by-one errors occur in the middle of a memory word.
                var halfIndex = (ushort)(size / 2 - 1);
                yield return new TestCaseData(halfIndex, bufferSizeBytes)
                    .SetName($"Size_{size}b_Half");
            }
        }
    }
    
    /// <summary>
    ///     Helper to calculate physical memory boundaries derived from the allocated buffer size.
    ///     Returns the total bit count and the maximum valid zero-based index.
    /// </summary>
    private static (int TotalBits, int MaxIndex) GetPhysicalLimits(int bufferSize)
    {
        var totalBits = bufferSize * 8;
        return (totalBits, totalBits - 1);
    }
}