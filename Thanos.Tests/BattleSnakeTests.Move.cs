namespace Thanos.Tests;

public partial class BattleSnakeTests
{
    [Test]
    public unsafe void Move_OnDeadSnake_HasNoEffect()
    {
        // Arrange
        const ushort startingHead = 42;
        _sut->Initialize(startingHead, Capacity);
    
        // Act: Kill the snake and attempt to move it
        _sut->Kill();
        _sut->Move(999, false, 10);
    
        // Assert
        const int expectedLength = 1; // Length should remain 1 since the snake is dead
        AsserState(true, expectedLength);
        
        const int expectedTailIndex = 0; // The Tail index should remain 0 since the snake is dead
        AssertHeadAndTail(startingHead, expectedTailIndex, 1);
        
        var body = _sut->Body;
        var actualHead = body[0];
        
        Assert.That(actualHead, Is.EqualTo(startingHead), $"Mismatch at index {0}: expected {startingHead}, found {actualHead}");
        for(var i = 1; i < Capacity - 1; i++) Assert.That(body[i], Is.EqualTo(EmptyCell), $"Body index {i} should be 0, but found {body[i]}");
    }
    
    [Test]
    public unsafe void Move_WhenTakingDamage_DiesWhenHealthReachesZero()
    {
        // Arrange
        const ushort startingHead = 50;
        const int damagePerMove = 10;
    
        _sut->Initialize(startingHead, Capacity);
    
        var initialHealth = _sut->Health;
    
        var dieBound = initialHealth / damagePerMove;
    
        // Act
        for (var i = 1; i <= dieBound; i++) _sut->Move((ushort)(startingHead + i), false, damagePerMove);
    
        // Assert
        const int expectedLength = 1; // Length should remain 1 since the snake is dead
        AsserState(true, expectedLength);
        
        var totalMoves = dieBound - 1; // Total moves made before death, less one because the loop starts from 1
        var expectedHead = (ushort)(startingHead + totalMoves);
        var expectedTailIndex = totalMoves; 
        AssertHeadAndTail(expectedHead, expectedTailIndex, dieBound);
        
        // Check the garbage body
        AssertBody(_sut->Body, startingHead, totalMoves);
    }
    
    [Test]
    public unsafe void Move_WithoutEating_TakesDamageAndTailMoves()
    {
        // Arrange
        const int totalMovesWithEating = 1;
        const int totalMovesWithoutEating = 10;
        const ushort startingHead = 10;
    
        _sut->Initialize(startingHead, Capacity);

        var startingHealth = _sut->Health;

        // Act
        _sut->Move(startingHead + 1, true);
        for (var i = 2; i <= totalMovesWithoutEating; i++) _sut->Move((ushort)(startingHead + i), false);
    
        // Assert
        const int expectedLength = 2; // Length should increase by 1 since the snake has eaten once
        const int movesWithoutEating = totalMovesWithoutEating - 1; // decrease by 1 because the loop starts from 2
        var expectedHealth = startingHealth - movesWithoutEating; // Health decreases by 1 for each move without eating
        AsserState(false, expectedLength, expectedHealth);
        
        const int totalMoves = totalMovesWithEating + movesWithoutEating;
        const ushort expectedHead = startingHead + totalMoves;
        const int expectedTailIndex = totalMoves - 1; 
        AssertHeadAndTail(expectedHead, expectedTailIndex, totalMoves);
        
        Print(_sut->Body);
        
        // Check the garbage body
        AssertBody(_sut->Body, startingHead, totalMoves);
    }
    
    // [Test]
    // public unsafe void Move_WhenDamageIsFatal_StateStopsUpdating()
    // {
    //     // Arrange
    //     const ushort startingHead = 1001;
    //
    //     _sut->Initialize(startingHead, Capacity);
    //
    //     var startingLength = _sut->Length;
    //     var startingTailIndex = _sut->TailIndex;
    //
    //     // Act
    //     _sut->Move(35, false, 1000);
    //
    //     // Assert
    //     AssertHeadAndTail(0, startingHead, startingTailIndex);
    // }

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
        const ushort startingHead = 1;

        _sut->Initialize(startingHead, Capacity);

        var startingTailIndex = _sut->TailIndex;

        // Act: Move the snake, eating at each step to fill the buffer
        for (var i = 1; i <= totalMoves; i++) _sut->Move((ushort)(startingHead + i), true);
        
        // Assert
        AsserState(false, Capacity, 100);
        
        var expectedLength = (ushort)(startingHead + totalMoves);
        var expectedTailIndex = (startingTailIndex + totalMoves + 1) & (Capacity - 1);
        AssertHeadAndTail(expectedLength, expectedTailIndex, totalMoves);
        
        AssertSaturatedBody(_sut->Body, Capacity, expectedLength, expectedTailIndex);
    }

    private unsafe void AsserState(bool expectedDead, int expectedLength, int? expectedHealth = null)
    {
        Assert.Multiple(() =>
        {
            Assert.That(_sut->Dead, Is.EqualTo(expectedDead), expectedDead ? "Snake should be dead." : "Snake should be alive.");
            Assert.That(_sut->Health, expectedDead ? Is.LessThanOrEqualTo(0) : Is.EqualTo(expectedHealth), $"Health should be {expectedHealth}.");
            Assert.That(_sut->Length, Is.EqualTo(expectedLength), $"Length should be {expectedLength}.");
        });
    }
    
    private unsafe void AssertHeadAndTail(ushort expectedHead, int expectedTailIndex, int steps)
    {
        Assert.Multiple(() =>
        {
            Assert.That(_sut->Head, Is.EqualTo(expectedHead), $"Head should be {expectedHead} after {steps} moves.");
            Assert.That(_sut->TailIndex, Is.EqualTo(expectedTailIndex), $"TailIndex should be {expectedTailIndex} after {steps} moves.");
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
        for(var i = 0; i < Capacity; i++) Console.WriteLine($"Body[{i}] = {body[i]}");
    }
}