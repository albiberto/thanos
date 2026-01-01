using Thanos.SourceGen;
using static NUnit.Framework.Assert;

namespace Thanos.Tests.Integration.SnakeSystem;

public partial class SnakesSystemTests
{
    [TestCaseSource(nameof(SystemScenarios))]
    public void Initialize_WhenCalled_ShouldResetAllActiveSnakes(SnakesSystemTestContext ctx)
    {
        using (ctx)
        {
            // Arrange
            var system = ctx.Build();

            // Setup: Dirty the state of all active snakes
            for (var i = 0; i < ctx.ActiveCount; i++) system[i].Initialize(new Snake($"s{i}", 100, [1, 2, 3]));

            // Pre-Assert: Verify setup was effective
            That(system[0].Length, Is.EqualTo(3), "Setup failed to dirty memory.");

            // Act
            system.Initialize();

            // Assert
            for (var i = 0; i < ctx.ActiveCount; i++)
            {
                var snake = system[i];
                That(snake.Length, Is.Zero, $"Snake {i} length was not reset.");
                That(snake.Head, Is.Zero, $"Snake {i} head was not reset.");
                That(snake.IsDead, Is.True, $"Snake {i} should be dead (HP 0).");
            }
        }
    }

    [TestCaseSource(nameof(SystemScenarios))]
    public unsafe void Constructor_WhenInitialized_ShouldMapSnakesToSequentialMemoryBlocks(SnakesSystemTestContext ctx)
    {
        using (ctx)
        {
            // Act
            // (Build creates the struct which calculates pointers)
            var system = ctx.Build();

            // Assert
            // Verify strict memory stride between sequential snakes.
            // This ensures that Indexer logic inside SnakesSystem matches physical layout.
            for (var i = 0; i < ctx.ActiveCount - 1; i++)
            {
                var ptrCurrent = ctx.GetSnakePointer(i);
                var ptrNext = ctx.GetSnakePointer(i + 1);

                var actualDistance = ptrNext - ptrCurrent;
                var expectedStride = (long)ctx.Layout.SnakeStride.Next;

                That(actualDistance, Is.EqualTo(expectedStride), $"Stride mismatch between snake {i} and {i + 1}.");
            }
        }
    }
}