using Thanos.War.Structures;

namespace Thanos.Tests.Integration.Bitboard;

/// <summary>
///     Integration tests for the <see cref="Bitboard" /> structure.
///     Validates memory safety, SIMD operations, and logical bit manipulation
///     across various board sizes and alignment boundaries.
/// </summary>
[TestFixture]
[Parallelizable(ParallelScope.All)]
public partial class BitboardTests
{
    /// <summary>
    ///     Generates test cases covering a wide range of bitboard sizes.
    ///     Ensures coverage for Scalar, Vector128, and Vector256 paths.
    /// </summary>
    public static IEnumerable<TestCaseData> TestDimensions
    {
        get
        {
            // Range: From tiny boards (2 bits) to massive ones (2048 bits).
            // This guarantees we hit every switch-case in the SIMD logic/unrolling.
            int[] logicalBitSizes = [2, 4, 8, 16, 32, 64, 96, 128, 192, 256, 320, 384, 448, 512, 1024, 2048];

            foreach (var size in logicalBitSizes)
            {
                // Allocation logic: Round up to nearest 64-bit (8-byte) word.
                // This mimics the production allocation strategy for aligned access.
                var bufferSizeBytes = (size + 63) / 64 * 8;

                var physicalBits = (ushort)(bufferSizeBytes * 8);
                var physicalMaxIndex = (ushort)(physicalBits - 1);

                // Scenario A: Physical Boundary Stress
                // Logic uses the full allocated buffer. Padding is zero.
                yield return new TestCaseData(physicalMaxIndex, bufferSizeBytes)
                    .SetName($"Size_{size}b_PhysicalMax");

                // Scenario B: Logical Boundary (Real Usage)
                // Logic uses a subset. The remaining bits are 'padding' and MUST stay zero.
                var logicalMaxIndex = (ushort)(size - 1);
                yield return new TestCaseData(logicalMaxIndex, bufferSizeBytes)
                    .SetName($"Size_{size}b_LogicalMax");

                // Scenario C: Mid-word Check
                // Sanity check to ensure no off-by-one errors inside a ulong word.
                var halfIndex = (ushort)(size / 2 - 1);
                yield return new TestCaseData(halfIndex, bufferSizeBytes)
                    .SetName($"Size_{size}b_Half");
            }
        }
    }

    /// <summary>
    ///     Helper to calculate physical memory boundaries derived from the allocated buffer size.
    /// </summary>
    private static (int TotalBits, int MaxIndex) GetPhysicalLimits(int bufferSize)
    {
        var totalBits = bufferSize * 8;
        return (totalBits, totalBits - 1);
    }
}