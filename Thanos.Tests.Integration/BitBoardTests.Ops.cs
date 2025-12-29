using System.Numerics;
using Thanos.War.Structures;
using static NUnit.Framework.Assert;

namespace Thanos.Tests.Integration;

public partial class BitboardTests
{
    [TestCaseSource(nameof(TestDimensions))]
    public void Clear_ShouldZeroEntireMemoryRange(ushort _, int bufferSize)
    {
        // Arrange
        var buffer = new byte[bufferSize];
        Array.Fill<byte>(buffer, 0xFF); // Setup: All bits SET (1)
        var bb = new Bitboard(buffer);

        // Pre-Assert
        var (physicalBits, _) = GetPhysicalLimits(bufferSize);
        That(bb.PopCount(), Is.EqualTo(physicalBits), "Pre-condition failed: Buffer not full.");

        // Act
        bb.Clear();

        // Assert
        That(bb.PopCount(), Is.Zero, "PopCount should be zero after Clear.");
        
        foreach (var b in buffer)
            if (b != 0)
                Fail($"Memory not zeroed. Found byte: {b:X2}");
    }

    [TestCaseSource(nameof(TestDimensions))]
    public void Or_WithComplementaryPatterns_ShouldResultInFullSet(ushort _, int bufferSize)
    {
        // Setup: Complementary Patterns
        // Pattern A: 01010101 (0x55)
        // Pattern B: 10101010 (0xAA)
        // Expected:  11111111 (0xFF)

        var buffer1 = new byte[bufferSize];
        Array.Fill(buffer1, (byte)0x55);
        var buffer2 = new byte[bufferSize];
        Array.Fill(buffer2, (byte)0xAA);

        var bitboard1 = new Bitboard(buffer1);
        var bitboard2 = new Bitboard(buffer2);

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
    public void Xor_WithInversePatterns_ShouldResultInDifference(ushort _, int bufferSize)
    {
        // Setup: Inversion Logic
        // Pattern A: 11111111 (0xFF)
        // Pattern B: 10101010 (0xAA)
        // Expected:  01010101 (0x55)

        var buffer1 = new byte[bufferSize];
        Array.Fill(buffer1, (byte)0xFF);
        var buffer2 = new byte[bufferSize];
        Array.Fill(buffer2, (byte)0xAA);

        var bitboard1 = new Bitboard(buffer1);
        var bitboard2 = new Bitboard(buffer2);

        // Act
        bitboard1.Xor(bitboard2);

        // Assert
        var (physicalBits, _) = GetPhysicalLimits(bufferSize);
        var expectedCount = physicalBits / 2; // 0x55 has 4 bits set per byte (half total)
        That(bitboard1.PopCount(), Is.EqualTo(expectedCount), "XOR operation PopCount mismatch.");

        foreach (var b in buffer1)
            if (b != 0x55)
                Fail($"Integrity check failed. Expected 0x55, found {b:X2}.");
    }

    [TestCaseSource(nameof(TestDimensions))]
    public void PopCount_ShouldMatch_KnownStaticPatterns(ushort _, int bufferSize)
    {
        var buffer = new byte[bufferSize];
        var bitboard = new Bitboard(buffer);
        var (physicalBits, _) = GetPhysicalLimits(bufferSize);

        // Case 1: Empty
        Array.Clear(buffer, 0, buffer.Length);
        That(bitboard.PopCount(), Is.Zero, "PopCount failed for empty board.");

        // Case 2: Full (0xFF)
        Array.Fill(buffer, (byte)0xFF);
        That(bitboard.PopCount(), Is.EqualTo(physicalBits), "PopCount failed for full board.");

        // Case 3: Alternating (0xAA => 4 bits/byte)
        Array.Fill(buffer, (byte)0xAA);
        That(bitboard.PopCount(), Is.EqualTo(physicalBits / 2), "PopCount failed for 0xAA pattern.");
    }

    [TestCaseSource(nameof(TestDimensions))]
    public void PopCount_ShouldMatch_NaiveCalculation_OnRandomData(ushort _, int bufferSize)
    {
        var buffer = new byte[bufferSize];
        var rng = new Random(42); // Fixed seed for reproducibility
        rng.NextBytes(buffer);

        var bitboard = new Bitboard(buffer);

        // Act
        var actual = bitboard.PopCount();

        // Assert
        var expected = buffer.Sum(b => BitOperations.PopCount(b));
        That(actual, Is.EqualTo(expected), "PopCount mismatch against naive calculation.");
    }
}