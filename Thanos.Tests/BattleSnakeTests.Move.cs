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
    //
    [Test]
    public unsafe void Move_WithoutEating_TakesDamageAndTailMoves()
    {
        // Arrange
        const int totalMovesWithoutEating = 10;
        const ushort startingHead = 10;
        
        _sut->Initialize(startingHead, Capacity);
        
        var startingLength = _sut->Length;  // Should be 1
        var startingHealth = _sut->Health;  // Should be 100
        var startingTailIndex = _sut->TailIndex; // Should be 0

        // Act
        _sut->Move(startingHead + 1, true);
        for (var i = 2; i <= totalMovesWithoutEating + 1; i++) _sut->Move((ushort)(startingHead + i), false);

        // Calculate total moves made
        const int totalMoves = totalMovesWithoutEating + 1;

        // Assert
        Assert.Multiple(() =>
        {
            // 1. State assertions
            Assert.That(_sut->Dead, Is.False, $"Snake should not be dead after eating once and {totalMovesWithoutEating} moves without eating.");
            Assert.That(_sut->Health, Is.EqualTo(startingHealth - totalMovesWithoutEating), $"Health should be {100 - totalMovesWithoutEating} (100 from eating, then -{totalMovesWithoutEating} from moves without eating).");
            Assert.That(_sut->Length, Is.EqualTo(startingLength + 1), $"Length should be {startingLength + 1} (starting length + 1 from eating on first move).");
        
            // 2. Head and Tail assertions
            Assert.That(_sut->Head, Is.EqualTo(startingHead + totalMoves), $"Head should be at position {startingHead + totalMoves} after {totalMoves} total moves.");
        
            var expectedTailIndex = (startingTailIndex + totalMovesWithoutEating) % Capacity;
            Assert.That(_sut->TailIndex, Is.EqualTo(expectedTailIndex), $"TailIndex should be {expectedTailIndex} (started at {startingTailIndex}, moved {totalMovesWithoutEating} times as snake has length {startingLength + 1}).");

            // 3. Body content assertions
            AssertBodyContent(startingHead + totalMoves, _sut->Length);
        });
    }

    [Test]
    public unsafe void Move_WhenDamageIsFatal_StateStopsUpdating()
    {
        // Arrange
        const ushort startingHead = 1001;

        _sut->Initialize(startingHead, Capacity);
        
        var startingLength = _sut->Length;

        // Act
        _sut->Move(35, false, 1000); // Fatal damage

        // Assert
        Assert.Multiple(() =>
        {
            // 1. State assertions
            Assert.That(_sut->Health, Is.LessThanOrEqualTo(0), "Health should be lesser then 0 after snake dies.");
            Assert.That(_sut->Length, Is.EqualTo(startingLength), "The length should not change if the snake dies.");

            // 2. Head and Tail assertions
            Assert.That(_sut->Head, Is.EqualTo(startingHead), "The head position should not change if the snake dies.");
            Assert.That(_sut->TailIndex, Is.EqualTo(0), "After being full and eating once more, TailIndex should move to 1.");

            // 3. Body content assertions
            Assert.That(_sut->Body[0], Is.EqualTo(startingHead), $"Body segment at index 0 must contains the head.");
        });
    }

    [Test]
    public unsafe void Move_WhenBufferIsSaturatedByEating_MaintainsCorrectState()
    {
        // Arrange
        const ushort startingHead = 1000;

        _sut->Initialize(startingHead, Capacity);

        // Act
        // Fill the buffer completely by eating 'Capacity' times.
        // The snake starts at length 1, so it needs 'Capacity - 1' meals to become full.
        // Then, one more meal to force the tail to move.
        for (var i = 1; i <= Capacity; i++) _sut->Move((ushort)(startingHead + i), true);

        // Assert
        Assert.Multiple(() =>
        {
            // 1. State assertions
            Assert.That(_sut->Dead, Is.False, "Snake should not be dead after eating to full capacity.");
            Assert.That(_sut->Health, Is.EqualTo(100), "Health should be 100 after eating.");
            Assert.That(_sut->Length, Is.EqualTo(Capacity), "Length should be at maximum capacity.");

            // 2. Head and Tail assertions
            Assert.That(_sut->Head, Is.EqualTo(startingHead + Capacity), "Head should be at the final position.");
            Assert.That(_sut->TailIndex, Is.EqualTo(1), "After being full and eating once more, TailIndex should move to 1.");

            // 3. Body content assertions
            AssertSaturatedBodyContent(startingHead, Capacity);
        });
    }

    private unsafe void AssertSaturatedBodyContent(int startingHead, int lenght)
    {
        for (var j = 0; j < Capacity; j++)
        {
            // Body[0] has been overwritten by the last growth move (the 255th).
            // The buffer has wrapped around. The sequence of stored positions is: [pos_255, pos_0, pos_1, pos_2, ..., pos_254]
            // Body[j] contains the head position of the (j-1)th move, E.g.: Body[1] contains startingHead, Body[2] contains startingHead + 1, etc.
            var expectedValue = j == 0
                ? startingHead + lenght - 1
                : (ushort)(startingHead + j - 1);

            Assert.That(_sut->Body[j], Is.EqualTo(expectedValue), $"Body segment at index {j} has an incorrect value.");
        }
    }
    
    private unsafe void AssertBodyContent(int startingHead, int lenght)
    {
        for (var j = startingHead; j <= lenght; j++)
        {
            var expectedValue = startingHead + j;
            Assert.That(_sut->Body[j], Is.EqualTo(expectedValue), $"Body segment at index {j} has an incorrect value.");
        }
    }
}