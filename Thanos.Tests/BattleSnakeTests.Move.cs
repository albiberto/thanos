namespace Thanos.Tests;

public partial class BattleSnakeTests
{
    [Test]
    public unsafe void Move_OnDeadSnake_HasNoEffect()
    {
        // Arrange
        const ushort startingHead = 42;

        _sut->Initialize(startingHead, capacity);
        _sut->Kill(); // Killing the snake is part of the setup

        // Calculate the expected final state
        const bool expectedIsDead = true;
        const int expectedLength = 1;
        const int expectedHealth = 0;
        const ushort expectedHead = startingHead;
        const int expectedTailIndex = 0;

        // Act
        _sut->Move(999, false, 10); // This move should have no effect

        // Assert
        AssertFinalState(expectedIsDead, expectedLength, expectedHealth, expectedHead, expectedTailIndex);
        AssertBody(_sut->Body, startingHead, 0); // Body should not change
    }

    [Test]
    public unsafe void Move_WhenTakingDamage_DiesWhenHealthReachesZero()
    {
        // Arrange
        const ushort startingHead = 50;
        const int damagePerMove = 10;
        const int initialHealth = 100;
        _sut->Initialize(startingHead, capacity);

        // Calculate the expected final state
        const int movesToDie = initialHealth / damagePerMove;
        const int successfulMoves = movesToDie - 1; // State freezes before the fatal move

        const bool expectedIsDead = true;
        const int expectedLength = 1; // Never ate
        const int expectedHealth = 0; // Or <= 0
        const ushort expectedHead = startingHead + successfulMoves;
        const int expectedTailIndex = successfulMoves; // Tail moves with each successful move

        // Act
        for (var i = 1; i <= movesToDie; i++) _sut->Move((ushort)(startingHead + i), false, damagePerMove);

        // Assert
        AssertFinalState(expectedIsDead, expectedLength, expectedHealth, expectedHead, expectedTailIndex);
        AssertBody(_sut->Body, startingHead, successfulMoves);
    }

    [Test]
    public unsafe void Move_WithoutEating_TakesDamageAndTailMoves()
    {
        // Arrange
        const ushort startingHead = 10;
        const int eatingMoves = 1;
        const int nonEatingMoves = 9; // Execute 9 moves without eating
        _sut->Initialize(startingHead, capacity);

        // Calculate the expected final state
        const int totalMoves = eatingMoves + nonEatingMoves;
        const bool expectedIsDead = false;
        const int expectedLength = 1 + eatingMoves; // Grows by 1 only
        const int expectedHealth = 100 - nonEatingMoves; // Damage only for non-eating moves
        const ushort expectedHead = startingHead + totalMoves;
        const int movesThatAdvanceTail = totalMoves - eatingMoves; // Tail advances only when not growing
        const int expectedTailIndex = movesThatAdvanceTail;

        // Act
        _sut->Move(startingHead + 1, true); // Eat once
        for (var i = 1; i <= nonEatingMoves; i++) _sut->Move((ushort)(startingHead + 1 + i), false);

        // Assert
        AssertFinalState(expectedIsDead, expectedLength, expectedHealth, expectedHead, expectedTailIndex);
        AssertBody(_sut->Body, startingHead, totalMoves);
    }

    [Test]
    [TestCase(1)]
    [TestCase(1.25)]
    [TestCase(2)]
    [TestCase(1.5)]
    [TestCase(3)]
    [TestCase(3.75)]
    [TestCase(4)]
    public unsafe void Move_WhenEatingContinuously_MaintainsCorrectState(double percentage)
    {
        var totalMoves = (int)(capacity * percentage);
        
        // Arrange
        const ushort startingHead = 1;
        _sut->Initialize(startingHead, capacity);

        // Calculate the expected final state
        const bool expectedIsDead = false;
        const int expectedHealth = 100;
        var expectedLength = Math.Min(capacity, 1 + totalMoves);
        var expectedHead = (ushort)(startingHead + totalMoves);
        var movesThatAdvanceTail = totalMoves >= capacity ? totalMoves - (capacity - 1) : 0;
        var expectedTailIndex = movesThatAdvanceTail & (capacity - 1);

        // Act
        for (var i = 1; i <= totalMoves; i++) _sut->Move((ushort)(startingHead + i), true);

        // Assert
        AssertFinalState(expectedIsDead, expectedLength, expectedHealth, expectedHead, expectedTailIndex);
        AssertSaturatedBody(_sut->Body, expectedLength, expectedHead, _sut->TailIndex);
    }

    private unsafe void AssertFinalState(bool expectedDead, int expectedLength, int expectedHealth, ushort expectedHead, int expectedTailIndex)
    {
        Assert.Multiple(() =>
        {
            // General state
            Assert.That(_sut->Dead, Is.EqualTo(expectedDead), expectedDead ? "Snake should be dead." : "Snake should be alive.");
            Assert.That(_sut->Length, Is.EqualTo(expectedLength), $"Length should be {expectedLength}.");
            Assert.That(_sut->Health, expectedDead ? Is.LessThanOrEqualTo(0) : Is.EqualTo(expectedHealth), $"Health should be {expectedHealth}.");

            // Position
            Assert.That(_sut->Head, Is.EqualTo(expectedHead), $"Head should be at {expectedHead}.");
            Assert.That(_sut->TailIndex, Is.EqualTo(expectedTailIndex), $"TailIndex should be at {expectedTailIndex}.");
        });
    }

    private static unsafe void AssertBody(ushort* body, int startingHead, int totalMoves)
    {
        Assert.That(body[0], Is.EqualTo(startingHead), $"Mismatch at index {0}: expected {startingHead}, found {body[0]}");
        for (var i = 1; i <= totalMoves; i++) Assert.That(body[i], Is.EqualTo(startingHead + i - 1), $"Mismatch at index {i}: expected {startingHead + i}, found {body[i]}");
    }

    private static unsafe void AssertSaturatedBody(ushort* body, int capacity, int head, int tailIndex)
    {
        // 1. Calculate the expected starting value at tail position, and the capacity mask
        var expectedValue = (ushort)(head - (capacity - 1)) - 1;
        var capacityMask = capacity - 1;

        // 2. Verify all elements in the circular buffer
        Assert.Multiple(() =>
        {
            for (var i = 0; i < capacity; i++)
            {
                // 3. Calculate the buffer index to check, starting from the tail
                var bufferIndex = (tailIndex + i) & capacityMask;

                // 4. Assert that the value matches the expected sequence
                Assert.That(body[bufferIndex], Is.EqualTo(expectedValue), $"Mismatch at index {bufferIndex}: expected {expectedValue}, found {body[bufferIndex]}");

                expectedValue++;
            }
        });
    }

    private unsafe void Print(ushort* body)
    {
        for (var i = 0; i < capacity; i++) Console.WriteLine($"Body[{i}] = {body[i]}");
    }
}