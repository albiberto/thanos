using Thanos.War.Structures;
using static NUnit.Framework.Assert;

namespace Thanos.Tests.Integration;

public partial class BitboardTests
{
    [TestCaseSource(nameof(TestDimensions))]
    public void Set_WhenCalledOnEmptyBuffer_ShouldSetBitAndPreservePadding(ushort lastIndex, int bufferSize)
    {
        // Arrange
        var buffer = new byte[bufferSize]; // Implicitly 0x00
        var bitboard = new Bitboard(buffer);

        // Act
        for (ushort i = 0; i <= lastIndex; i++)
        {
            bitboard.Set(i);

            // Immediate sanity check (fail fast)
            if (!bitboard.IsSet(i)) Fail($"Immediate failure: Bit {i} was not set.");
        }

        // Assert
        var (_, physicalMax) = GetPhysicalLimits(bufferSize);

        // 1. Logical Zone Check (Must be 1)
        for (var i = 0; i <= lastIndex; i++)
            if (!bitboard.IsSet((ushort)i))
                Fail($"Logic mismatch: Bit {i} should be SET.");

        // 2. Padding Zone Check (Must stay 0 - Safety Boundary)
        // This proves that SIMD operations or bit-shifts didn't bleed into reserved memory.
        for (var i = lastIndex + 1; i <= physicalMax; i++)
            if (bitboard.IsSet((ushort)i))
                Fail($"Memory corruption: Bit {i} (padding) was incorrectly SET.");

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
        var bitboard = new Bitboard(buffer);

        // Act
        for (ushort i = 0; i <= lastIndex; i++)
        {
            bitboard.Unset(i);
            if (bitboard.IsSet(i)) Fail($"Immediate failure: Bit {i} was not unset.");
        }

        // Assert
        var (physicalBits, physicalMax) = GetPhysicalLimits(bufferSize);

        // 1. Logical Zone Check (Must be 0)
        for (var i = 0; i <= lastIndex; i++)
            if (bitboard.IsSet((ushort)i))
                Fail($"Logic mismatch: Bit {i} should be UNSET.");

        // 2. Padding Zone Check (Must stay 1 - Safety Boundary)
        // Since we started with 0xFF, padding must remain 1.
        for (var i = lastIndex + 1; i <= physicalMax; i++)
            if (!bitboard.IsSet((ushort)i))
                Fail($"Memory corruption: Bit {i} (padding) was incorrectly CLEARED.");

        // 3. Population Count Check
        var expectedCount = physicalBits - (lastIndex + 1);
        That(bitboard.PopCount(), Is.EqualTo(expectedCount), $"PopCount mismatch. Expected {expectedCount}.");
    }

    [TestCaseSource(nameof(TestDimensions))]
    public void CopyTo_WhenInvoked_ShouldProduceExactBitwiseClone(ushort _, int bufferSize)
    {
        // Arrange
        var sourceRaw = new byte[bufferSize];
        var destinationRaw = new byte[bufferSize];

        // Setup: Distinct patterns to verify overwrite
        // Source: 01010101 (0x55)
        // Dest:   10101010 (0xAA)
        Array.Fill(sourceRaw, (byte)0x55);
        Array.Fill(destinationRaw, (byte)0xAA);

        var sourceBitboard = new Bitboard(sourceRaw);
        var destinationBitboard = new Bitboard(destinationRaw);

        // Act
        sourceBitboard.CopyTo(destinationBitboard);

        // Assert
        var sourceBuffer = sourceBitboard.Buffer;
        var destinationBuffer = destinationBitboard.Buffer;

        That(destinationBuffer.Length, Is.EqualTo(sourceBuffer.Length), "Buffer length mismatch.");
        for (var i = 0; i < sourceBuffer.Length; i++) That(destinationBuffer[i], Is.EqualTo(sourceBuffer[i]), $"Memory mismatch at ulong index {i}. Expected {sourceBuffer[i]:X16}, got {destinationBuffer[i]:X16}.");
    }
}