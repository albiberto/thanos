namespace Thanos.Tests;

/// <summary>
///     Contains tests related to killing the BattleSnake.
/// </summary>
public partial class BattleSnakeTests
{
    /// <summary>
    ///     Verifies that calling Kill sets the health to zero without modifying other properties.
    /// </summary>
    [Test]
    public unsafe void Kill_KillSnake_SetHealthToZeroAndDoesntTouchOtherProperties()
    {
        // Arrange: capture initial state
        var initialLength = _sut->Length;
        var initialHead = _sut->Head;
        var initialTailIndex = _sut->TailIndex;

        // Act: kill the snake
        _sut->Kill();

        // Assert: verify only health is changed
        Assert.Multiple(() =>
        {
            Assert.That(_sut->Dead, Is.True, "Snake should be dead.");
            Assert.That(_sut->Length, Is.EqualTo(initialLength), "Length should not change on a dead snake.");
            Assert.That(_sut->Head, Is.EqualTo(initialHead), "Head should not change on a dead snake.");
            Assert.That(_sut->TailIndex, Is.EqualTo(initialTailIndex), "TailIndex should not change on a dead snake.");
        });
    }
}