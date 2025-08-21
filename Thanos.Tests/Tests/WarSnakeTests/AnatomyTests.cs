using Thanos.War.Snake;

namespace Thanos.Tests.Tests.WarSnakeTests;

[TestFixtureSource(nameof(Capacities))]
public class AnatomyTests(int capacity)
{
    public static int[] Capacities { get; } = [4, 8, 16, 32, 64, 128, 256, 512, 1024];

    [Test(Description = "Ensures the constructor correctly assigns the minimal state.")]
    public void Constructor_ShouldInitializeMinimalStateCorrectly()
    {
        // Arrange
        var length = capacity / 2;

        // Act
        var anatomy = new Anatomy(capacity, length);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(anatomy.Capacity, Is.EqualTo(capacity));
            Assert.That(anatomy.Length, Is.EqualTo(length));
            Assert.That(anatomy.TailIndex, Is.Zero);
        });
    }

    // --- Tests for Computed Properties ---

    // [TestCase(0, 5, 4, TestName = "HeadIndex: Should be correct in a normal case")]
    // [TestCase(15, 2, 0, TestName = "HeadIndex: Should wrap around correctly")]
    // [TestCase(0, 1, 0, TestName = "HeadIndex: Should equal TailIndex when length is 1")]
    // public void HeadIndex_ShouldBeCalculatedCorrectly(int tailIndex, int length, int expectedHeadIndex)
    // {
    //     // Arrange
    //     var anatomy = new Anatomy(capacity, length, tailIndex);
    //     
    //     // Act & Assert
    //     Assert.That(anatomy.HeadIndex, Is.EqualTo(expectedHeadIndex));
    // }
    //
    // [TestCase(0, 5, 5, TestName = "NextHeadIndex: Should be correct in a normal case")]
    // [TestCase(15, 1, 0, TestName = "NextHeadIndex: Should wrap around correctly")]
    // public void NextHeadIndex_ShouldBeCalculatedCorrectly(int tailIndex, int length, int expectedNextHeadIndex)
    // {
    //     // Arrange
    //     var anatomy = new Anatomy(capacity, length, tailIndex);
    //
    //     // Act & Assert
    //     Assert.That(anatomy.NextHeadIndex, Is.EqualTo(expectedNextHeadIndex));
    // }
    //
    // // --- Tests for State Mutation Methods ---
    //
    // [Test]
    // public void PopTail_WhenCalled_ShouldIncrementTailIndex()
    // {
    //     // Arrange
    //     var anatomy = new Anatomy(capacity, 5, 1);
    //
    //     // Act
    //     anatomy.PopTail();
    //
    //     // Assert
    //     Assert.That(anatomy.TailIndex, Is.EqualTo(2));
    // }
    //
    // [Test]
    // public void PopTail_WhenAtBufferEnd_ShouldWrapTailIndexToZero()
    // {
    //     // Arrange
    //     var initialTailIndex = capacity - 1;
    //     var anatomy = new Anatomy(capacity, 5, initialTailIndex);
    //
    //     // Act
    //     anatomy.PopTail();
    //
    //     // Assert
    //     Assert.That(anatomy.TailIndex, Is.EqualTo(0));
    // }
    //
    // [Test]
    // public void IncrementLength_WhenBelowCapacity_ShouldIncrementLengthByOne()
    // {
    //     // Arrange
    //     var initialLength = capacity / 2;
    //     var anatomy = new Anatomy(capacity, initialLength, 0);
    //
    //     // Act
    //     anatomy.IncrementLength();
    //
    //     // Assert
    //     Assert.That(anatomy.Length, Is.EqualTo(initialLength + 1));
    // }
    //
    // [Test]
    // public void IncrementLength_WhenAtCapacity_ShouldNotChangeLength()
    // {
    //     // Arrange
    //     var anatomy = new Anatomy(capacity, capacity, 0);
    //
    //     // Act
    //     anatomy.IncrementLength();
    //
    //     // Assert
    //     Assert.That(anatomy.Length, Is.EqualTo(capacity));
    // }
}