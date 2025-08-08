namespace Thanos.Tests;

/// <summary>
/// Unit tests for the BattleSnake Move behavior under different conditions.
/// </summary>
public partial class BattleSnakeTests
{
    /// <summary>
    /// Tests that when the snake moves without eating, its length remains constant
    /// and the tail index advances.
    /// </summary>
    [Test]
    public unsafe void Move_WhenNotEating_LengthIsConstantAndTailMoves()
    {
        // Arrange initial state
        var initialLength = _sut->Length;
        var initialTailIndex = _sut->TailIndex;
        const ushort newHeadPosition = 999; // Arbitrary new head position

        // Act: move without eating
        _sut->Move(newHeadPosition, false);

        // Assert: check length, head, and tail index
        Assert.Multiple(() =>
        {
            Assert.That(_sut->Length, Is.EqualTo(initialLength), "Length should not change when not eating.");
            Assert.That(_sut->Head, Is.EqualTo(newHeadPosition), "Head should update to the new position.");

            // Expect tail index to increment (circular buffer)
            var expectedTailIndex = (initialTailIndex + 1) % @case.Capacity;
            Assert.That(_sut->TailIndex, Is.EqualTo(expectedTailIndex), "TailIndex should advance by one.");
        });
    }

    /// <summary>
    /// Tests that when the snake eats, its length increases and tail index stays the same.
    /// </summary>
    [Test]
    public unsafe void Move_WhenEating_LengthIncrementsAndTailIsConstant()
    {
        // Skip test if already full
        if (@case.Body.Length >= @case.Capacity)
        {
            Assert.Pass("Cannot test eating when snake is already at full capacity.");
            return;
        }

        // Arrange initial state
        var initialLength = _sut->Length;
        var initialTailIndex = _sut->TailIndex;
        const ushort newHeadPosition = 888;

        // Act: move with eating
        _sut->Move(newHeadPosition, true);

        // Assert: check length increase, tail unchanged, health reset
        Assert.Multiple(() =>
        {
            Assert.That(_sut->Length, Is.EqualTo(initialLength + 1), "Length should increment by 1 when eating.");
            Assert.That(_sut->Head, Is.EqualTo(newHeadPosition));
            Assert.That(_sut->TailIndex, Is.EqualTo(initialTailIndex), "TailIndex should NOT change when eating.");
            Assert.That(_sut->Health, Is.EqualTo(100), "Health should reset to 100 when eating.");
        });
    }

    /// <summary>
    /// Tests that health is reduced correctly when the snake takes damage during a move.
    /// </summary>
    [Test]
    public unsafe void Move_ReducesHealth_WhenTakingDamage()
    {
        // Arrange initial health
        var initialHealth = _sut->Health;
        const int damage = 15;

        // Act: move with damage
        _sut->Move(777, false, damage);

        // Assert: health should decrease by damage + 1 (base move penalty)
        Assert.That(_sut->Health, Is.EqualTo(initialHealth - damage - 1));
    }

    /// <summary>
    /// Tests that repeated eating fills the snake to max capacity, maintaining consistent state.
    /// </summary>
    [Test]
    public unsafe void Move_WhenEatingUntilFull_ReachesMaxCapacityAndStateIsCorrect()
    {
        // Arrange: calculate number of required moves to fill capacity
        var movesToFill = @case.Capacity - @case.Body.Length;

        // If already full, confirm and exit
        if (movesToFill <= 0)
        {
            Assert.That(_sut->Length, Is.EqualTo(@case.Capacity));
            Assert.Pass("Snake started at full capacity, test is valid.");
            return;
        }

        // Get current head position
        var initialHead = @case.Body[0];

        // Act: simulate multiple eating moves to reach full capacity
        for (var i = 1; i <= movesToFill; i++)
        {
            var nextHeadPosition = (ushort)(initialHead + i); // Sequential head positions
            _sut->Move(nextHeadPosition, true);
        }

        // Assert: verify final state
        Assert.Multiple(() =>
        {
            // Should reach full capacity
            Assert.That(_sut->Length, Is.EqualTo(@case.Capacity), "Snake should be at full capacity.");

            // Health should be reset by last eating move
            Assert.That(_sut->Health, Is.EqualTo(100), "Health should be reset to 100 after eating.");

            // Head should be at the last calculated position
            var expectedHead = (ushort)(initialHead + movesToFill);
            Assert.That(_sut->Head, Is.EqualTo(expectedHead), "Head is not at the final expected position.");

            // The Tail should not have moved since the snake only grew
            var expectedTailIndex = @case.Body.Length > 0 ? @case.Body.Length - 1 : 0;
            Assert.That(_sut->TailIndex, Is.EqualTo(expectedTailIndex), "TailIndex should not have moved during growth phase.");
        });
    }
}
