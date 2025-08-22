using Thanos.War.Snake;

namespace Thanos.Tests.Tests.WarSnakeTests;

/// <summary>
/// Contains all unit tests for the Anatomy struct, verifying its state and behavior
/// across various buffer capacities and conditions.
/// </summary>
[TestFixtureSource(nameof(Capacities))]
public class AnatomyTests(int capacity)
{
    /// <summary>
    /// Provides a set of different capacities to run all tests against, ensuring robustness.
    /// </summary>
    public static int[] Capacities { get; } = [8, 16, 32, 128, 256, 512, 1024];

    // =================================================================
    // Constructor Tests
    // =================================================================

    [TestCase(2, Description = "Case: Half full")]
    [TestCase(4, Description = "Case: A quarter full")]
    [Test(Description = "Ensures the constructor correctly initializes state for a non-full buffer.")]
    public void Constructor_WhenNotFull_ShouldInitializeStateCorrectly(int ratio)
    {
        // Arrange
        var length = capacity / ratio;

        // Act
        var anatomy = new Anatomy(capacity, length);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(anatomy.Capacity, Is.EqualTo(capacity), "Capacity should be set from constructor.");
            Assert.That(anatomy.Length, Is.EqualTo(length), "Length should be set from constructor.");
            Assert.That(anatomy.TailIndex, Is.Zero, "Default TailIndex should always be 0 on creation.");
            Assert.That(anatomy.CapacityMask, Is.EqualTo(capacity - 1), "CapacityMask should be capacity - 1.");
            Assert.That(anatomy.IsFull, Is.False, "IsFull must be false when length is less than capacity.");

            var expectedHeadIndex = length - 1;
            Assert.That(anatomy.HeadIndex, Is.EqualTo(expectedHeadIndex), "HeadIndex should be calculated correctly.");

            var expectedNextHeadIndex = length;
            Assert.That(anatomy.NextHeadIndex, Is.EqualTo(expectedNextHeadIndex), "NextHeadIndex should be calculated correctly.");
        });
    }

    [Test(Description = "Ensures the constructor correctly initializes state for a full buffer.")]
    public void Constructor_WhenAtFullCapacity_ShouldInitializeStateCorrectly()
    {
        // Arrange
        var length = capacity;

        // Act
        var anatomy = new Anatomy(capacity, length);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(anatomy.Capacity, Is.EqualTo(capacity), "Capacity should match the provided value.");
            Assert.That(anatomy.Length, Is.EqualTo(capacity), "Length should match the capacity.");
            Assert.That(anatomy.TailIndex, Is.Zero, "Default TailIndex should be 0.");
            Assert.That(anatomy.IsFull, Is.True, "IsFull must be true when length equals capacity.");
            
            var expectedHeadIndex = (capacity - 1) & (capacity - 1);
            Assert.That(anatomy.HeadIndex, Is.EqualTo(expectedHeadIndex), "HeadIndex should be the last index of the buffer.");

            Assert.That(anatomy.NextHeadIndex, Is.Zero, "NextHeadIndex should wrap around to 0 when full.");
        });
    }

    // =================================================================
    // PopTail Method Tests
    // =================================================================

    [TestCase(2, Description = "Case: Half full")]
    [TestCase(4, Description = "Case: A quarter full")]
    [Test(Description = "Ensures PopTail correctly increments TailIndex when the buffer is not full.")]
    public void PopTail_OnNotFullBuffer_ShouldIncrementTailIndex(int ratio)
    {
        // Arrange
        var length = capacity / ratio;
        var anatomy = new Anatomy(capacity, length);

        // Act
        anatomy.PopTail();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(anatomy.TailIndex, Is.EqualTo(1), "TailIndex should increment from 0 to 1.");
            Assert.That(anatomy.Length, Is.EqualTo(length), "Length should not be affected by PopTail.");
            Assert.That(anatomy.IsFull, Is.False, "IsFull flag should not change.");

            var expectedHeadIndex = (1 + length - 1) & (capacity - 1);
            Assert.That(anatomy.HeadIndex, Is.EqualTo(expectedHeadIndex), "HeadIndex should be recalculated based on the new TailIndex.");
        });
    }

    [Test(Description = "Ensures PopTail correctly updates HeadIndex when the buffer is full.")]
    public void PopTail_OnFullBuffer_ShouldCorrectlyShiftWindow()
    {
        // Arrange
        var anatomy = new Anatomy(capacity, capacity);

        // Act
        anatomy.PopTail(); // Tail moves from 0 to 1

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(anatomy.TailIndex, Is.EqualTo(1), "TailIndex should move to the next position.");
            Assert.That(anatomy.Length, Is.EqualTo(capacity), "Length should remain at capacity.");
            Assert.That(anatomy.IsFull, Is.True, "IsFull flag should remain true.");

            // With TailIndex=1 and Length=capacity, HeadIndex should now be 0.
            Assert.That(anatomy.HeadIndex, Is.EqualTo(0), "HeadIndex should now be at the start of the buffer.");
            Assert.That(anatomy.NextHeadIndex, Is.EqualTo(1), "NextHeadIndex should now be where the new TailIndex is.");
        });
    }
    
    [Test(Description = "Ensures calling PopTail 'capacity' times returns the state to its origin.")]
    public void PopTail_WhenCalledCapacityTimes_ShouldReturnToInitialState()
    {
        // Arrange
        var anatomy = new Anatomy(capacity, capacity);
        var initialState = anatomy;

        // Act
        for(var i = 0; i < capacity; i++) anatomy.PopTail();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(anatomy.TailIndex, Is.EqualTo(initialState.TailIndex), "TailIndex should complete a full circle and return to its initial state.");
            Assert.That(anatomy.HeadIndex, Is.EqualTo(initialState.HeadIndex), "HeadIndex should also return to its initial state.");
            Assert.That(anatomy.NextHeadIndex, Is.EqualTo(initialState.NextHeadIndex), "NextHeadIndex should also return to its initial state.");
            Assert.That(anatomy.Length, Is.EqualTo(initialState.Length), "Length should remain unchanged.");
        });
    }

    // =================================================================
    // IncrementLength Method Tests
    // =================================================================

    [Test(Description = "Ensures IncrementLength increases length by one when there is available capacity.")]
    public void IncrementLength_WhenNotFull_ShouldIncreaseLengthByOne()
    {
        // Arrange
        var length = capacity / 2;
        var anatomy = new Anatomy(capacity, length);

        // Act
        anatomy.IncrementLength();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(anatomy.Length, Is.EqualTo(length + 1), "Length should increment by one.");
            Assert.That(anatomy.TailIndex, Is.Zero, "TailIndex should not change when length is incremented.");
            Assert.That(anatomy.Capacity, Is.EqualTo(capacity), "Capacity should not change.");
            
            var expectedHeadIndex = length & (capacity - 1); // (0 + (length + 1) - 1) = length
            Assert.That(anatomy.HeadIndex, Is.EqualTo(expectedHeadIndex), "HeadIndex should be recalculated for the new length.");
        });
    }

    [Test(Description = "Ensures IncrementLength has no effect when the buffer is already at full capacity.")]
    public void IncrementLength_WhenAtCapacity_ShouldHaveNoEffect()
    {
        // Arrange
        var anatomy = new Anatomy(capacity, capacity);
        var initialState = anatomy; // Struct copy for comparison

        // Act
        anatomy.IncrementLength();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(anatomy.Length, Is.EqualTo(initialState.Length), "Length should not change when already at capacity.");
            Assert.That(anatomy.TailIndex, Is.EqualTo(initialState.TailIndex), "TailIndex should not change.");
            Assert.That(anatomy.HeadIndex, Is.EqualTo(initialState.HeadIndex), "HeadIndex should not change.");
            Assert.That(anatomy.IsFull, Is.True, "IsFull should remain true.");
        });
    }
}