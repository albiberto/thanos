using System.Runtime.InteropServices;
using Thanos.Memory;
using Thanos.War.Snake;

namespace Thanos.Tests.Tests.WarSnakeTests;

/// <summary>
///     Contains all unit tests for the WarSnakes ref struct.
///     These tests verify that the indexer correctly slices a larger memory block
///     and provides an accurate WarSnake view for each snake.
/// </summary>
[TestFixture]
public class WarSnakesTests
{
    // =================================================================
    // Indexer Tests
    // =================================================================

    [TestCase(0, TestName = "Indexer access for the first snake (index 0)")]
    [TestCase(1, TestName = "Indexer access for a middle snake (index 1)")]
    [TestCase(3, TestName = "Indexer access for the last snake (index 3)")]
    [Test(Description = "Ensures the indexer correctly slices the memory block and returns an accurate view of the snake at a specific index.")]
    public void Indexer_ShouldReturnCorrectSnakeViewForIndex(int index)
    {
        // =================================================================
        // ARRANGE
        // =================================================================

        // 1. Define a memory layout for a small game (e.g., 4 snakes).
        //    This defines the stride and sizes needed to manually write to memory.
        var layout = new MemoryLayout(25, 4);
        var snakesMemory = new byte[layout.SnakesSize];

        // 2. Define the exact state of the "target" snake that we will manually write into memory.
        var targetHealth = new Health(77);
        var targetAnatomy = new Anatomy(16, 5); // capacity=16, length=5
        var targetBody = new ushort[] { 10, 20, 30, 40, 50 };

        // 3. Manually write this state into the correct memory "slot" within the larger buffer.
        var offset = index * layout.SnakeStride;

        // Write Health and Anatomy structs directly into the byte buffer.
        MemoryMarshal.Write(snakesMemory.AsSpan(offset), ref targetHealth);
        MemoryMarshal.Write(snakesMemory.AsSpan(offset + layout.SnakeHealthSize), ref targetAnatomy);

        // Get the slice for the body and copy the data there.
        var bodyDestinationSpan = MemoryMarshal.Cast<byte, ushort>(snakesMemory.AsSpan(offset + layout.SnakeHeaderSize));
        targetBody.AsSpan().CopyTo(bodyDestinationSpan);

        // 4. Create the WarSnakes instance that we are going to test.
        var snakes = new WarSnakes(layout, snakesMemory);

        // ACT
        // Use the indexer to get a view of the snake at the target index.
        var snakeView = snakes[index];

        // ASSERT
        // Verify that the properties of the returned view correctly reflect the data we manually wrote to memory.
        Assert.That(snakeView.Dead, Is.False, "Snake's dead status should match the written Health data.");
        Assert.That(snakeView.Length, Is.EqualTo(targetAnatomy.Length), "Snake's length should match the written Anatomy data.");
        Assert.That(snakeView.Tail, Is.EqualTo(targetBody[0]), "Snake's tail should be the first element of the body data.");
        Assert.That(snakeView.Head, Is.EqualTo(targetBody[^1]), "Snake's head should be the last element of the body data.");
    }
}