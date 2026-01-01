using static NUnit.Framework.Assert;

namespace Thanos.Tests.Integration.Bitboard;

public partial class BitboardTests
{
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