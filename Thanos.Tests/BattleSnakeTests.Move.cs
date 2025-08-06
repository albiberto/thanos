namespace Thanos.Tests;

public partial class BattleSnakeTests
{
    // [Test]
    // public void Move_WhenEating_HealthBecomes100AndLengthIncreases()
    // {
    //     // Arrange
    //     var initialLength = _sut.Length;
    //     var initialTailIndex = _sut.TailIndex;
    //
    //     // Act
    //     _sut.Move(15, true, 1); // damage is ignored
    //
    //     Assert.Multiple(() =>
    //     {
    //         // Assert
    //         Assert.That(_sut.Health, Is.EqualTo(100), "Health should be reset to 100 when eating.");
    //         Assert.That(_sut.Length, Is.EqualTo(initialLength + 1), "Length should increase by 1.");
    //     });
    //     Assert.Multiple(() =>
    //     {
    //         Assert.That(_sut.Head, Is.EqualTo(15), "The head should be at the new position.");
    //         Assert.That(_sut.TailIndex, Is.EqualTo(initialTailIndex), "The tail index should not move when eating.");
    //     });
    // }

    [Test]
    public unsafe void Move_WithoutEating_TakesDamageAndTailMoves()
    {
        // Arrange
        const int totalMovesWithoutEating = 10;
        const ushort startingHead = 10;

        _sut->Initialize(startingHead, Capacity);

        var startingLength = _sut->Length; // Should be 1
        var startingHealth = _sut->Health; // Should be 100
        var startingTailIndex = _sut->TailIndex; // Should be 0

        // Act
        _sut->Move(startingHead + 1, true);
        for (var i = 2; i <= totalMovesWithoutEating + 1; i++) _sut->Move((ushort)(startingHead + i), false);

        // Calculate total moves made
        const int totalMoves = totalMovesWithoutEating + 1;
        const int expectedLength = 2; // After eating once, the snake should have length 2

        // Assert
        AssertAll(false, _sut->Health, expectedLength, totalMoves, startingHead, startingTailIndex);
    }

    [Test]
    public unsafe void Move_WhenDamageIsFatal_StateStopsUpdating()
    {
        // Arrange
        const ushort startingHead = 1001;

        _sut->Initialize(startingHead, Capacity);

        var startingLength = _sut->Length;
        var startingTailIndex = _sut->TailIndex;

        // Act
        _sut->Move(35, false, 1000);

        // Assert
        AssertAll(true, 0, startingLength, 0, startingHead, startingTailIndex);
    }

    [Test]
    public unsafe void Move_WhenBufferIsSaturatedByEating_MaintainsCorrectState()
    {
        // Arrange
        const ushort startingHead = 1000;

        _sut->Initialize(startingHead, Capacity);

        var startingLength = _sut->Length;
        var startingTailIndex = _sut->TailIndex;

        // Act
        // Fill the buffer completely by eating 'Capacity' times.
        // The snake starts at length 1, so it needs 'Capacity - 1' meals to become full.
        // Then, one more meal to force the tail to move.
        for (var i = 1; i <= Capacity; i++) _sut->Move((ushort)(startingHead + i), true);

        // Assert
        AssertAll(false, 100, Capacity, Capacity, startingHead, startingTailIndex);
    }

    private unsafe void AssertAll(bool ExpectedDead, int ExpectedHealth, int expectedLength, int totalMoves, ushort startingHead, int startingTailIndex)
    {
        // 1. State assertions
        Assert.Multiple(() =>
        {
            Assert.That(_sut->Dead, Is.EqualTo(ExpectedDead), ExpectedDead ? "Snake should be dead." : "Snake should be alive.");
            Assert.That(_sut->Health, ExpectedDead ? Is.LessThanOrEqualTo(0) : Is.EqualTo(ExpectedHealth), $"Health should be {ExpectedHealth}.");
            Assert.That(_sut->Length, Is.EqualTo(expectedLength), $"Length should be {expectedLength}.");
        });

        // 2. Head and Tail assertions
        var expectedHead = startingHead + totalMoves;
        var expectedTailIndex = (startingTailIndex + totalMoves - (_sut->Length - 1)) % Capacity;
        Assert.Multiple(() =>
        {
            Assert.That(_sut->Head, Is.EqualTo(expectedHead), $"Head should be at position {expectedHead} after {totalMoves} total moves.");
            Assert.That(_sut->TailIndex, Is.EqualTo(expectedTailIndex), $"TailIndex should be {expectedTailIndex} (started at {startingTailIndex}, moved {totalMoves} times as snake has length {expectedLength}).");
        });
        
        // 3. Body content assertions
        AssertBodyContent(startingHead + totalMoves, _sut->Length);
    }

    private unsafe void AssertBodyContent(int startingHead, int length)
    {
        var iterationLimit = length > Capacity ? Capacity : length;

        Assert.Multiple(() =>
        {
            for (var j = startingHead; j < iterationLimit; j++)
            {
                // Calculate the expected value based on whether the buffer is saturated
                var expectedValue = length > Capacity
                    ? j == 0
                        ? (ushort)(startingHead + length - 1) // Wrapped around: last position at index 0
                        : (ushort)(startingHead + j - 1) // Saturated: offset by -1
                    : (ushort)(startingHead + j); // Non-saturated: simple sequential

                Assert.That(_sut->Body[j], Is.EqualTo(expectedValue), $"Body segment at index {j} has an incorrect value.");
            } 
        });
    }
}