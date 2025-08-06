namespace Thanos.Tests;

public partial class BattleSnakeTests
{
    [Test]
    public unsafe void Move_OnDeadSnake_HasNoEffect()
    {
        // Arrange
        const ushort startingHead = 42;
        _sut->Initialize(startingHead, Capacity);
    
        // Uccidi il serpente
        _sut->Kill();
        Assert.That(_sut->Dead, Is.True, "Precondition: Snake must be dead.");

        // Memorizza lo stato da "morto"
        var deadStateHealth = _sut->Health;
        var deadStateLength = _sut->Length;
        var deadStateHead = _sut->Head;
        var deadStateTailIndex = _sut->TailIndex;

        // Act
        // Prova a muovere il serpente morto
        _sut->Move(999, false, 10);

        // Assert
        const int totalMoves = 0; // The snake is dead, so no moves should be counted.
        AssertAll(true, 0, deadStateLength, totalMoves, startingHead, deadStateTailIndex);
    }
    
    [Test]
    public unsafe void Move_WhenTakingDamage_DiesWhenHealthReachesZero()
    {
        // Arrange
        const ushort startingHead = 50;
        const int damagePerMove = 10;
        
        _sut->Initialize(startingHead, Capacity);

        var startingLength = _sut->Length;
        var initialHealth = _sut->Health; 
        var startingTailIndex = _sut->TailIndex;
        
        var totalMoves = initialHealth / damagePerMove;
        
        // Act
        for (var i = 1; i <= totalMoves; i++) _sut->Move((ushort)(startingHead + i), false, damagePerMove);

        // Assert
        AssertAll(true, 0, startingLength, totalMoves -1, startingHead, startingTailIndex);
    }

    [Test]
    public unsafe void Move_WithoutEating_TakesDamageAndTailMoves()
    {
        // Arrange
        const int totalMovesWithEating = 1;
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
    [TestCase(Capacity)]
    [TestCase(Capacity + Capacity / 2)]
    [TestCase(Capacity * 2)]
    [TestCase(Capacity * 2 + Capacity / 3)]
    [TestCase(Capacity * 3)]
    [TestCase(Capacity * 3 + Capacity / 4)]
    [TestCase(Capacity * 4)]
    [TestCase(Capacity * 4 + Capacity / 5)]
    public unsafe void Move_WhenBufferIsSaturatedByEating_MaintainsCorrectState(int totalMoves)
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
        for (var i = 1; i <= totalMoves; i++) _sut->Move((ushort)(startingHead + i), true);

        // Assert
        AssertAll(false, 100, Capacity, totalMoves, startingHead, startingTailIndex);
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