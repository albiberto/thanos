using static NUnit.Framework.Assert;

namespace Thanos.Tests.Integration.Bitboard;

public partial class BitboardTests
{
    [TestCaseSource(nameof(TestDimensions))]
    public void Set_WhenCalledOnEmptyBuffer_ShouldSetBitAndPreservePadding(ushort lastIndex, int bufferSize)
    {
        // Arrange
        var buffer = new byte[bufferSize]; // Implicitly 0x00
        var bitboard = new War.Structures.Bitboard(buffer);

        // Act
        for (ushort i = 0; i <= lastIndex; i++)
        {
            bitboard.Set(i);

            // Immediate sanity check (fail fast)
            That(bitboard.IsSet(i), Is.True, $"Immediate failure: Bit {i} was not set.");
        }

        // Assert
        var (_, physicalMax) = GetPhysicalLimits(bufferSize);

        // 1. Logical Zone Check (Must be 1)
        for (var i = 0; i <= lastIndex; i++) That(bitboard.IsSet((ushort)i), Is.True, $"Logic mismatch: Bit {i} should be SET.");

        // 2. Padding Zone Check (Must stay 0 - Safety Boundary)
        // This proves that SIMD operations or bit-shifts didn't bleed into reserved memory.
        for (var i = lastIndex + 1; i <= physicalMax; i++) That(bitboard.IsSet((ushort)i), Is.False, $"Memory corruption: Bit {i} (padding) was incorrectly SET.");

        // 3. Population Count Check
        var expectedCount = lastIndex + 1;
        That(bitboard.PopCount(), Is.EqualTo(expectedCount), $"PopCount mismatch. Expected {expectedCount}.");
    }

    [TestCaseSource(nameof(TestDimensions))]
    public void Unset_WhenCalledOnFullBuffer_ShouldClearBitAndPreservePadding(ushort lastIndex, int bufferSize)
    {
        // Arrange
        var buffer = new byte[bufferSize];
        Array.Fill<byte>(buffer, 0xFF); // Setup: Physically full (All 1s)
        var bitboard = new War.Structures.Bitboard(buffer);

        // Act
        for (ushort i = 0; i <= lastIndex; i++)
        {
            bitboard.Unset(i);
            That(bitboard.IsSet(i), Is.False, $"Immediate failure: Bit {i} was not unset.");
        }

        // Assert
        var (physicalBits, physicalMax) = GetPhysicalLimits(bufferSize);

        // 1. Logical Zone Check (Must be 0)
        for (var i = 0; i <= lastIndex; i++) That(bitboard.IsSet((ushort)i), Is.False, $"Logic mismatch: Bit {i} should be UNSET.");

        // 2. Padding Zone Check (Must stay 1 - Safety Boundary)
        // Since we started with 0xFF, padding must remain 1.
        for (var i = lastIndex + 1; i <= physicalMax; i++) That(bitboard.IsSet((ushort)i), Is.True, $"Memory corruption: Bit {i} (padding) was incorrectly CLEARED.");

        // 3. Population Count Check
        var expectedCount = physicalBits - (lastIndex + 1);
        That(bitboard.PopCount(), Is.EqualTo(expectedCount), $"PopCount mismatch. Expected {expectedCount}.");
    }

    [TestCaseSource(nameof(TestDimensions))]
    public void Set_WhenCalledRepeatedlyOnSameBit_ShouldRemainSet(ushort _, int bufferSize)
    {
        // Arrange
        var buffer = new byte[bufferSize];
        var bitboard = new War.Structures.Bitboard(buffer);
        const ushort targetBit = 1;

        // Act
        // Prima attivazione
        bitboard.Set(targetBit);
        var popCountAfterFirst = bitboard.PopCount();

        // Seconda attivazione (Idempotenza)
        bitboard.Set(targetBit);
        var popCountAfterSecond = bitboard.PopCount();

        // Assert
        That(bitboard.IsSet(targetBit), Is.True, "Bit should be set.");
        That(popCountAfterFirst, Is.EqualTo(1), "PopCount should be 1 after first Set.");
        That(popCountAfterSecond, Is.EqualTo(1), "PopCount should remain 1 after second Set (Operation must be idempotent).");
    }

    [TestCaseSource(nameof(TestDimensions))]
    public void Unset_WhenCalledRepeatedlyOnEmptyBit_ShouldRemainUnset(ushort _, int bufferSize)
    {
        // Arrange
        var buffer = new byte[bufferSize];
        // Riempiamo tutto per testare l'unset su un bit specifico
        Array.Fill<byte>(buffer, 0xFF);
        var bitboard = new War.Structures.Bitboard(buffer);

        const ushort targetBit = 1;
        var maxBits = GetPhysicalLimits(bufferSize).TotalBits;

        // Act
        // Prima disattivazione
        bitboard.Unset(targetBit);
        var popCountAfterFirst = bitboard.PopCount();

        // Seconda disattivazione (Idempotenza)
        bitboard.Unset(targetBit);
        var popCountAfterSecond = bitboard.PopCount();

        // Assert
        That(bitboard.IsSet(targetBit), Is.False, "Bit should be unset.");
        That(popCountAfterFirst, Is.EqualTo(maxBits - 1), "PopCount should decrease by 1.");
        That(popCountAfterSecond, Is.EqualTo(maxBits - 1), "PopCount should remain stable after second Unset.");
    }
}