using static NUnit.Framework.Assert;

namespace Thanos.Tests.Integration.Bitboard;

public partial class BitboardTests
{
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

        var sourceBitboard = new War.Structures.Bitboard(sourceRaw);
        var destinationBitboard = new War.Structures.Bitboard(destinationRaw);

        // Act
        sourceBitboard.CopyTo(destinationBitboard);

        // Assert
        var sourceBuffer = sourceBitboard.Buffer;
        var destinationBuffer = destinationBitboard.Buffer;

        That(destinationBuffer.Length, Is.EqualTo(sourceBuffer.Length), "Buffer length mismatch.");
        for (var i = 0; i < sourceBuffer.Length; i++) That(destinationBuffer[i], Is.EqualTo(sourceBuffer[i]), $"Memory mismatch at ulong index {i}. Expected {sourceBuffer[i]:X16}, got {destinationBuffer[i]:X16}.");
    }
}