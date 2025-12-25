using Thanos.War.Structures;
using static NUnit.Framework.Assert;

namespace Thanos.Tests.Integration;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class BitboardTests
{
    public static IEnumerable<TestCaseData> TestDimensions
    {
        get
        {
            // Define the bit-counts we want to test, ranging from less than one ulong (32) to multiple ulongs (512).
            int[] logicalBitSizes = [32, 64, 96, 128, 192, 256, 512];

            foreach (var size in logicalBitSizes)
            {
                // CALCULATION:
                // Since Bitboard uses ulong* (64-bit pointers), memory must be aligned to 8 bytes.
                // Formula: Round up to the nearest multiple of 64 bits, then convert to bytes.
                // Example (Size 32): (32 + 63) / 64 = 1 ulong  -> 8 Bytes.
                // Example (Size 96): (96 + 63) / 64 = 2 ulongs -> 16 Bytes.
                var bufferSizeBytes = (size + 63) / 64 * 8;
                var physicalBits = (ushort)(bufferSizeBytes * 8);

                // --- CASE 1: Physical Memory Boundary (Safety Stress Test) ---
                // This tests the absolute limit of the allocated byte array.
                // Because we allocate in 64-bit chunks (ulongs), the Physical Memory is often larger than the Logical Size.
                //
                // Example for Size = 32:
                // - Logical Request: 32 bits.
                // - Physical Allocation: 64 bits (1 ulong / 8 bytes).
                // - Logical Max Index: 31.
                // - Physical Max Index: 63.
                var physicalBound = (ushort)(physicalBits - 1);
                yield return new TestCaseData(physicalBound, physicalBound, physicalBits, (byte)bufferSizeBytes)
                    .SetName($"Size_{size}b_PhysicalMax");

                // --- CASE 3: Logical Upper Bound (User Perspective) ---
                // This tests the exact number of bits requested by the "game" logic.
                // We verify that we can write up to the last bit defined by 'size'.
                //
                // Example for Size = 32:
                // - We expect valid indices from 0 to 31.
                // - Input here is 31.
                var logicalBound = (ushort)(size - 1);
                yield return new TestCaseData(logicalBound, physicalBound, physicalBits, (byte)bufferSizeBytes)
                    .SetName($"Size_{size}b_LogicalMax");

                // --- CASE 2: Half Capacity ---
                // Standard usage test, filling only half the board.
                var halfBound = (ushort)(size / 2 - 1);
                yield return new TestCaseData(halfBound, physicalBound, physicalBits, (byte)bufferSizeBytes)
                    .SetName($"Size_{size}b_Half");
            }
        }
    }
[TestCaseSource(nameof(TestDimensions))]
    public void StressTest_Set_WhenBufferIsEmpty(ushort upperBound, ushort physicalBound, ushort _, byte bufferSize)
    {
        // Arrange
        var buffer = new byte[bufferSize];
        Array.Clear(buffer, 0, buffer.Length); 
        var bb = new Bitboard(buffer);

        // Pre-Check
        if (bb.PopCount() != 0) Fail("Setup failure: Bitboard should be empty upon initialization.");

        // Act
        for (ushort i = 0; i <= upperBound; i++)
        {
            bb.Set(i);

            // Immediate check
            if (!bb.IsSet(i)) Fail($"Immediate failure: Failed to SET bit at index {i}.");
        }

        // Assert: Population Count
        var expected = upperBound + 1;
        var actual = bb.PopCount();

        That(actual, Is.EqualTo(expected), $"Population failure: Final count mismatch. Expected {expected} but was {actual}.");
        
        // Assert: Raw Memory Integrity (Full Scan)
        for (ushort i = 0; i <= physicalBound; i++)
            if (i <= upperBound)
            {
                // Logic Zone: Must be SET
                if (!bb.IsSet(i)) Fail($"Integrity failure: Bit {i} (Logical Zone) should be SET but was UNSET.");
            }
            else
            {
                // Extra Zone (Padding): Must be UNSET (remained clean)
                if (bb.IsSet(i)) Fail($"Integrity failure: Bit {i} (Extra Zone) was incorrectly SET.");
            }
    }

    [TestCaseSource(nameof(TestDimensions))]
    public void StressTest_Unset_WhenBufferIsFull(ushort upperBound, ushort physicalBound, ushort physicalBits, byte bufferSize)
    {
        // Arrange
        var buffer = new byte[bufferSize];
        Array.Fill<byte>(buffer, 0xFF); 
        var bb = new Bitboard(buffer);

        // Pre-Check
        if (bb.PopCount() != physicalBits) Fail($"Setup failure: Expected {physicalBits} physical bits set, but got {bb.PopCount()}.");

        // Act: Unset only the logical range
        for (ushort i = 0; i <= upperBound; i++)
        {
            bb.Unset(i);

            // Immediate check
            if (bb.IsSet(i)) Fail($"Immediate failure: Failed to UNSET bit at index {i}.");
        }

        // Assert: Population Count
        // Calculation: Total Physical - (Logical Bits Removed)
        var expected = physicalBits - (upperBound + 1);
        var actual = bb.PopCount();

        That(actual, Is.EqualTo(expected), $"Population failure: Unset cleared incorrect amount. Expected {expected} dirty bits remaining but was {actual}.");

        // Assert: Raw Memory Integrity (Full Scan)
        for (ushort i = 0; i <= physicalBound; i++)
            if (i <= upperBound)
            {
                // Logic Zone: Must be UNSET
                if (bb.IsSet(i)) Fail($"Integrity failure: Bit {i} (Logical Zone) should be UNSET but was SET.");
            }
            else
            {
                // Extra Zone (Padding): Must be SET (remained dirty)
                if (!bb.IsSet(i)) Fail($"Integrity failure: Bit {i} (Extra Zone) was incorrectly UNSET/CLEARED.");
            }
    }

    [TestCaseSource(nameof(TestDimensions))]
    public void StressTest_Clear_WhenBufferIsFull(ushort upperBound, ushort physicalBound, ushort physicalBits, byte bufferSize)
    {
        // Arrange
        var buffer = new byte[bufferSize];
        Array.Fill<byte>(buffer, 0xFF); 
        var bb = new Bitboard(buffer);

        // Pre-Check
        if (bb.PopCount() != physicalBits) Fail($"Setup failure: Expected {physicalBits} physical bits set, but got {bb.PopCount()}.");

        // Act
        bb.Clear();

        // Assert: Population Count
        const int expected = 0;
        var actual = bb.PopCount();

        That(actual, Is.EqualTo(expected), $"Population failure: Non-zero count after Clear(). Expected {expected} but was {actual}.");

        // Assert: Raw Memory Integrity (Double Check)
        for (var i = 0; i < buffer.Length; i++)
            if (buffer[i] != 0) Fail($"Integrity failure: Raw memory byte at index {i} is not zero.");
    }

[TestCaseSource(nameof(TestDimensions))]
    public void StressTest_Set_WhenBufferIsEmpty_Ascending(ushort upperBound, ushort physicalBound, ushort _, byte bufferSize)
    {
        // Arrange
        var buffer = new byte[bufferSize];
        Array.Clear(buffer, 0, buffer.Length); 
        var bb = new Bitboard(buffer);

        // Pre-Check
        if (bb.PopCount() != 0) Fail("Setup failure: Bitboard should be empty upon initialization.");

        // Act: Iterate 0 -> upperBound
        for (ushort i = 0; i <= upperBound; i++)
        {
            bb.Set(i);

            // Immediate check
            if (!bb.IsSet(i)) Fail($"Immediate failure: Failed to SET bit at index {i}.");
        }

        // Assert: Population Count
        var expected = upperBound + 1;
        var actual = bb.PopCount();

        That(actual, Is.EqualTo(expected), $"Population failure: Final count mismatch. Expected {expected} but was {actual}.");
    }

    [TestCaseSource(nameof(TestDimensions))]
    public void StressTest_Unset_WhenBufferIsFull_Descending(ushort upperBound, ushort physicalBound, ushort physicalBits, byte bufferSize)
    {
        // Arrange
        var buffer = new byte[bufferSize];
        Array.Fill<byte>(buffer, 0xFF); // Setup: Pieno Fisico
        var bb = new Bitboard(buffer);

        // Pre-Check
        if (bb.PopCount() != physicalBits) Fail($"Setup failure: Expected {physicalBits} physical bits set, but got {bb.PopCount()}.");

        // Act: Unset only the logical range, going BACKWARDS (upperBound -> 0)
        for (var i = (int)upperBound; i >= 0; i--)
        {
            var idx = (ushort)i;
            bb.Unset(idx);

            // Immediate check
            if (bb.IsSet(idx)) Fail($"Immediate failure: Failed to UNSET bit at index {idx}.");
        }

        // Assert: Population Count
        // Calculation: Total Physical - (Logical Bits Removed)
        var expected = physicalBits - (upperBound + 1);
        var actual = bb.PopCount();

        That(actual, Is.EqualTo(expected), $"Population failure: Unset cleared incorrect amount. Expected {expected} dirty bits remaining but was {actual}.");
    }
}