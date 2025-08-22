using Thanos.War.Grid;

namespace Thanos.Tests.Tests.WarGridTests;

/// <summary>
///     Contains all unit tests for the Bitboard ref struct.
///     These tests verify the correctness of bit manipulation operations (Set, Clear, IsSet),
///     ensuring there are no side effects and that edge cases are handled properly.
/// </summary>
[TestFixture]
public class BitboardTests
{
    // =================================================================
    // Core Functionality Tests
    // =================================================================

    [Test(Description = "Ensures that IsSet correctly returns false for any bit on a newly created (zeroed) bitboard.")]
    public void IsSet_OnEmptyBitboard_ShouldReturnFalse()
    {
        // Arrange
        var buffer = new ulong[4];
        var bitboard = new Bitboard(buffer);

        // Act & Assert
        Assert.That(bitboard.IsSet(42), Is.False, "IsSet should return false for any bit on an empty board.");
    }

    [Test(Description = "Ensures Set correctly turns a bit on and IsSet correctly reads its state.")]
    public void Set_And_IsSet_ShouldWorkForSingleBit()
    {
        // Arrange
        // A buffer of 4 ulongs provides 256 bits (4 * 64) for testing.
        var buffer = new ulong[4];
        var bitboard = new Bitboard(buffer);
        const ushort positionToSet = 75; // A test position within the second ulong segment.

        // Act
        bitboard.Set(positionToSet);

        // Assert
        Assert.That(bitboard.IsSet(positionToSet), Is.True, "The target bit should be reported as set.");

        // Also verify the underlying memory to confirm the bitmask logic.
        // Position 75 -> ulong index 1 (75 / 64), bit index 11 (75 % 64).
        const ulong expectedValue = 1UL << 11;
        Assert.That(buffer[1], Is.EqualTo(expectedValue), "The raw ulong in memory should match the expected bitmask.");
    }

    [Test(Description = "Ensures Clear correctly turns a previously set bit off.")]
    public void Clear_ShouldTurnOffSetBit()
    {
        // Arrange
        var buffer = new ulong[4];
        var bitboard = new Bitboard(buffer);
        const ushort position = 128; // The first bit of the third ulong segment.

        bitboard.Set(position);
        Assert.That(bitboard.IsSet(position), Is.True, "Pre-condition check: The bit must be set before it can be cleared.");

        // Act
        bitboard.Clear(position);

        // Assert
        Assert.That(bitboard.IsSet(position), Is.False, "The target bit should be reported as not set after being cleared.");
        Assert.That(buffer[2], Is.EqualTo(0UL), "The raw ulong in memory should be zero after its only bit is cleared.");
    }

    // =================================================================
    // Isolation Tests
    // =================================================================

    [Test(Description = "Verifies that operating on one bit does not affect other bits within the same ulong segment.")]
    public void Clear_ShouldNotAffectOtherBitsInSameUlongSegment()
    {
        // Arrange
        var buffer = new ulong[1];
        var bitboard = new Bitboard(buffer);
        const ushort positionToClear = 5;
        const ushort positionToKeep = 10;

        // Set two distinct bits within the same ulong.
        bitboard.Set(positionToClear);
        bitboard.Set(positionToKeep);

        // Act
        // Clear only one of the bits.
        bitboard.Clear(positionToClear);

        // Assert
        Assert.That(bitboard.IsSet(positionToClear), Is.False, "The bit at position 5 should have been cleared.");
        Assert.That(bitboard.IsSet(positionToKeep), Is.True, "The bit at position 10 should have been left untouched.");
    }

    // =================================================================
    // Edge Case Tests
    // =================================================================

    [TestCase((ushort)0, TestName = "Edge Case: First bit of the entire bitboard (pos 0)")]
    [TestCase((ushort)63, TestName = "Edge Case: Last bit of the first ulong segment (pos 63)")]
    [TestCase((ushort)64, TestName = "Edge Case: First bit of the second ulong segment (pos 64)")]
    [TestCase((ushort)255, TestName = "Edge Case: Last bit of the 256-bit test buffer (pos 255)")]
    [Test(Description = "Ensures that Set and Clear operations function correctly at critical boundary positions.")]
    public void Operations_ShouldWorkCorrectlyOnEdgeCases(ushort position)
    {
        // Arrange
        var buffer = new ulong[4]; // 256 bits to accommodate all test cases.
        var bitboard = new Bitboard(buffer);

        // Assert 1: Verify the initial state is 'off'.
        Assert.That(bitboard.IsSet(position), Is.False, "Bit should be off initially.");

        // Act 1: Set the bit.
        bitboard.Set(position);

        // Assert 2: Verify the bit is now 'on'.
        Assert.That(bitboard.IsSet(position), Is.True, "Bit should be on after Set().");

        // Act 2: Clear the bit.
        bitboard.Clear(position);

        // Assert 3: Verify the bit is 'off' again.
        Assert.That(bitboard.IsSet(position), Is.False, "Bit should be off again after Clear().");
    }
}