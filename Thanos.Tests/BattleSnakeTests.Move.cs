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
    // [Test]
    // public void Move_WithoutEating_TakesDamageAndTailMoves()
    // {
    //     // Arrange
    //     var initialHealth = _sut.Health;
    //     var initialLength = _sut.Length;
    //     var initialTailIndex = _sut.TailIndex;
    //
    //     // Act
    //     _sut.Move(25, false, 10);
    //
    //     // Assert
    //     Assert.Multiple(() =>
    //     {
    //         Assert.That(_sut.Health, Is.EqualTo(initialHealth - 10), "Health should decrease by the damage amount.");
    //         Assert.That(_sut.Length, Is.EqualTo(initialLength), "Length should not change.");
    //         Assert.That(_sut.Head, Is.EqualTo(25), "The head should be at the new position.");
    //         Assert.That(_sut.TailIndex, Is.Not.EqualTo(initialTailIndex), "The tail index should have changed.");
    //     });
    // }
    //
    // [Test]
    // public void Move_WhenDamageIsFatal_StateStopsUpdating()
    // {
    //     // Arrange
    //     _sut.Health = 5; // Set a low health for the test
    //     var initialHead = _sut.Head;
    //     var initialLength = _sut.Length;
    //
    //     // Act
    //     _sut.Move(35, false, 10); // Fatal damage
    //
    //     // Assert
    //     Assert.Multiple(() =>
    //     {
    //         Assert.That(_sut.Dead, Is.True, "The snake should be dead.");
    //         Assert.That(_sut.Health, Is.EqualTo(0), "Health should be 0 after Kill() is called.");
    //         Assert.That(_sut.Head, Is.EqualTo(initialHead), "The head position should not change if the snake dies.");
    //         Assert.That(_sut.Length, Is.EqualTo(initialLength), "The length should not change if the snake dies.");
    //     });
    // }

    [Test]
    public unsafe void Move_WhenBufferIsSaturatedByEating_MaintainsCorrectState()
    {
        // Arrange
        const ushort startingHead = 1000;
        const int capacity = 256; // The circolar buffer capacity for the snake.

        _sut->Initialize(startingHead, capacity);

        // Act
        // Fill the buffer completely by eating 'capacity' times.
        // The snake starts at length 1, so it needs 'capacity - 1' meals to become full.
        // Then, one more meal to force the tail to move.
        for (var i = 1; i <= capacity; i++) _sut->Move((ushort)(startingHead + i), true, 0);

        // Assert
        Assert.Multiple(() =>
        {
            // 1. State assertions
            Assert.That(_sut->Health, Is.EqualTo(100), "Health should be 100 after eating.");
            Assert.That(_sut->Length, Is.EqualTo(capacity), "Length should be at maximum capacity.");

            // 2. Head and Tail assertions
            Assert.That(_sut->Head, Is.EqualTo(startingHead + capacity), "Head should be at the final position.");
            Assert.That(_sut->TailIndex, Is.EqualTo(1), "After being full and eating once more, TailIndex should move to 1.");

            // 4. Body content assertions
            for (var j = 0; j < capacity; j++)
            {
                ushort expectedValue;

                // The buffer has wrapped around. The sequence of stored positions is: [pos_255, pos_0, pos_1, pos_2, ..., pos_254]
                if (j == 0)
                    // Body[0] has been overwritten by the last growth move (the 255th).
                    expectedValue = startingHead + capacity - 1; 
                else
                    // Body[j] contains the head position of the (j-1)th move, E.g.: Body[1] contains startingHead, Body[2] contains startingHead + 1, etc.
                    expectedValue = (ushort)(startingHead + j - 1);
                
                Assert.That(_sut->Body[j], Is.EqualTo(expectedValue), $"Body segment at index {j} has an incorrect value.");
            }
        });
    }
}