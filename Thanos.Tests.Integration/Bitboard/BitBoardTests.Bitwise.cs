using static NUnit.Framework.Assert;

namespace Thanos.Tests.Integration.Bitboard;

public partial class BitboardTests
{
    [TestCaseSource(nameof(TestDimensions))]
    public void Clear_WhenBufferIsFull_ShouldResetAllBitsToZero(ushort _, int bufferSize)
    {
        // Arrange
        var buffer = new byte[bufferSize];
        Array.Fill<byte>(buffer, 0xFF); // Setup: All bits SET (1)
        var bitboard = new War.Structures.Bitboard(buffer);

        // Pre-Assert: Verify strict starting condition
        var (physicalBits, _) = GetPhysicalLimits(bufferSize);
        That(bitboard.PopCount(), Is.EqualTo(physicalBits), "Pre-condition failed: Buffer not full.");

        // Act
        bitboard.Clear();

        // Assert
        That(bitboard.PopCount(), Is.Zero, "PopCount should be zero after Clear.");

        foreach (var b in buffer)
            if (b != 0)
                Fail($"Memory not zeroed. Found byte: {b:X2}");
    }

    [TestCaseSource(nameof(TestDimensions))]
    public void Or_WhenMergingComplementaryPatterns_ShouldResultInFullSet(ushort _, int bufferSize)
    {
        // Arrange
        // Pattern A: 01010101 (0x55)
        // Pattern B: 10101010 (0xAA)
        // Expected:  11111111 (0xFF) -> Union
        var buffer1 = new byte[bufferSize];
        Array.Fill(buffer1, (byte)0x55);

        var buffer2 = new byte[bufferSize];
        Array.Fill(buffer2, (byte)0xAA);

        var bitboard1 = new War.Structures.Bitboard(buffer1);
        var bitboard2 = new War.Structures.Bitboard(buffer2);

        // Act
        bitboard1.Or(bitboard2);

        // Assert
        var (physicalBits, _) = GetPhysicalLimits(bufferSize);

        That(bitboard1.PopCount(), Is.EqualTo(physicalBits), "OR operation failed to produce full mask (0xFF).");

        // Integrity check for Result (bb1)
        foreach (var b in buffer1)
            if (b != 0xFF)
                Fail($"Integrity check failed. Expected 0xFF, found {b:X2}.");

        // Immutability check for Source (bb2)
        foreach (var b in buffer2)
            if (b != 0xAA)
                Fail($"Source operand was modified. Expected 0xAA, found {b:X2}.");
    }

    [TestCaseSource(nameof(TestDimensions))]
    public void Xor_WhenApplyingInverseMask_ShouldToggleBits(ushort _, int bufferSize)
    {
        // Arrange
        // Pattern A: 11111111 (0xFF)
        // Pattern B: 10101010 (0xAA)
        // Expected:  01010101 (0x55) -> Difference
        var buffer1 = new byte[bufferSize];
        Array.Fill(buffer1, (byte)0xFF);

        var buffer2 = new byte[bufferSize];
        Array.Fill(buffer2, (byte)0xAA);

        var bitboard1 = new War.Structures.Bitboard(buffer1);
        var bitboard2 = new War.Structures.Bitboard(buffer2);

        // Act
        bitboard1.Xor(bitboard2);

        // Assert
        var (physicalBits, _) = GetPhysicalLimits(bufferSize);
        var expectedCount = physicalBits / 2; // 0x55 has 4 bits set per byte (half total)

        That(bitboard1.PopCount(), Is.EqualTo(expectedCount), "XOR operation PopCount mismatch.");

        // Integrity check for Result (bb1)
        foreach (var b in buffer1)
            if (b != 0x55)
                Fail($"Integrity check failed. Expected 0x55, found {b:X2}.");

        // Immutability check for Source (bb2)
        foreach (var b in buffer2)
            if (b != 0xAA)
                Fail($"Source operand was modified. Expected 0xAA, found {b:X2}.");
    }
}