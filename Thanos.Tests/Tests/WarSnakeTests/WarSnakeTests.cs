using Thanos.War.Snake;

namespace Thanos.Tests.Tests.WarSnakeTests;

/// <summary>
///     Contains all unit tests for the WarSnake ref struct.
///     These tests verify the constructors, state mutation through Move(),
///     and memory representation via GetSpans(), using a dedicated Harness for memory allocation.
/// </summary>
[TestFixture]
public class WarSnakeTests
{
    // =================================================================
    // Constructor Tests
    // =================================================================

    [Test(Description = "Ensures the main constructor correctly initializes all internal states.")]
    public void MainConstructor_ShouldInitializeAllStatesCorrectly()
    {
        // --- ARRANGE ---
        // 1. Define the initial game state.
        const int capacity = 16;
        const int initialHp = 90;
        const int snakeId = 42;
        var initialBody = new ushort[] { 1, 2, 3, 4 };

        // 2. Allocate raw memory using the Harness.
        var context = Harness.CreateTestContext(capacity);

        // --- ACT ---
        // 3. Initialize the memory by calling the main constructor.
        var snake = new WarSnake(ref context.Health, ref context.Anatomy, context.BodyBuffer, snakeId, initialHp, initialBody, capacity);

        // --- ASSERT ---
        Assert.That(snake.Id, Is.EqualTo(snakeId), "ID should be set from constructor.");
        Assert.That(snake.Length, Is.EqualTo(initialBody.Length), "Length should match the initial body's length.");
        Assert.That(snake.Dead, Is.False, "Snake should be alive with positive HP.");
        Assert.That(snake.Tail, Is.EqualTo(1), "Tail should be the first element of the initial body.");
        Assert.That(snake.Head, Is.EqualTo(4), "Head should be the last element of the initial body.");

        var bodySlice = context.BodyBuffer.AsSpan(0, initialBody.Length);
        Assert.That(bodySlice.ToArray(), Is.EqualTo(initialBody).AsCollection, "Initial body data should be copied to the underlying buffer.");
    }

    [Test(Description = "Ensures the 'viewer' constructor correctly attaches to a pre-existing state without modifying it.")]
    public void ViewerConstructor_ShouldCorrectlyViewExistingState()
    {
        // --- ARRANGE ---
        // 1. Manually create a specific game state in memory.
        var context = new Harness.SnakeTestContext
        {
            Health = new Health(50), // HP at 50%
            Anatomy = new Anatomy(16, 5), // Length 5, TailIndex 0
            BodyBuffer = new ushort[16]
        };
        // Manually place head and tail values in the buffer to match the Anatomy state.
        context.BodyBuffer[0] = 10; // Tail value at index 0
        context.BodyBuffer[4] = 99; // Head value at index (0 + 5 - 1) = 4

        // --- ACT ---
        // 2. Create a WarSnake view over this existing, pre-populated memory.
        var snake = new WarSnake(ref context.Health, ref context.Anatomy, context.BodyBuffer);

        // --- ASSERT ---
        // 3. Verify the view correctly reflects the manually created state.
        Assert.That(snake.Length, Is.EqualTo(5), "Length should reflect the existing Anatomy state.");
        Assert.That(snake.Dead, Is.False, "Dead status should reflect the existing Health state.");
        Assert.That(snake.Head, Is.EqualTo(99), "Head value should be read from the correct memory position.");
        Assert.That(snake.Tail, Is.EqualTo(10), "Tail value should be read from the correct memory position.");
    }

    // =================================================================
    // Move Method Tests
    // =================================================================

    [Test(Description = "Ensures Move() without eating shifts the snake and does not change its length.")]
    public void Move_WhenNotEating_ShouldShiftBody()
    {
        // Arrange
        var initialBody = new ushort[] { 10, 20, 30 };
        var context = Harness.CreateTestContext(16);
        var snake = new WarSnake(ref context.Health, ref context.Anatomy, context.BodyBuffer, 1, 100, initialBody, 16);

        // Act
        snake.Move(40, false, 1);

        // Assert
        Assert.That(snake.Length, Is.EqualTo(3), "Length should not change when not eating.");
        Assert.That(snake.Tail, Is.EqualTo(20), "Tail should advance to the next segment.");
        Assert.That(snake.Head, Is.EqualTo(40), "Head should be the new value.");
        Assert.That(context.BodyBuffer[3], Is.EqualTo(40), "The new head value should be written to the buffer.");
    }

    [Test(Description = "Ensures Move() when eating increases the snake's length and does not shift the tail.")]
    public void Move_WhenEating_ShouldIncreaseLength()
    {
        // Arrange
        var initialBody = new ushort[] { 10, 20, 30 };
        var context = Harness.CreateTestContext(16);
        var snake = new WarSnake(ref context.Health, ref context.Anatomy, context.BodyBuffer, 1, 100, initialBody, 16);

        // Act
        snake.Move(40, true, 0);

        // Assert
        Assert.That(snake.Length, Is.EqualTo(4), "Length should increase by one.");
        Assert.That(snake.Tail, Is.EqualTo(10), "Tail should not move when eating.");
        Assert.That(snake.Head, Is.EqualTo(40), "Head should be the new value.");
    }
}