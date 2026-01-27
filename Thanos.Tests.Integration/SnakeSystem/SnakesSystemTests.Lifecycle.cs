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
            // We use specific positions [1, 2, 3] to ensure Bitboard has bits set
            for (var i = 0; i < ctx.ActiveCount; i++)
                system[i].Initialize(new($"s{i}", 100, [1, 2, 3]));

            // Pre-Assert: Verify setup was effective
            // We must confirm the Bitboard is actually dirty before testing the cleanup
            That(system[0].Length, Is.EqualTo(3), "Setup failed to dirty Queue.");
            That(system[0].Body.PopCount(), Is.EqualTo(3), "Setup failed to dirty Bitboard.");

            // Act
            system.Initialize();

            // Assert
            for (var i = 0; i < ctx.ActiveCount; i++)
            {
                var snake = system[i];

                // 1. Verify Queue Reset
                That(snake.Length, Is.Zero, $"Snake {i} length was not reset.");
                That(snake.Head, Is.Zero, $"Snake {i} head was not reset.");

                // 2. Verify Life Reset
                That(snake.IsDead, Is.True, $"Snake {i} should be dead (HP 0).");

                // 3. Verify Bitboard Reset
                // This assertion ensures the bitboard memory range is physically zeroed
                That(snake.Body.PopCount(), Is.Zero, $"Snake {i} bitboard was not cleared.");
            }
        }
    }

    [TestCaseSource(nameof(SystemScenarios))]
    public void Initialize_WhenCalled_ShouldResetIndices_ButLeaveQueueBufferDirty(SnakesSystemTestContext ctx)
    {
        using (ctx)
        {
            // Arrange
            var system = ctx.Build();
            var snake = system[0];

            // Setup: Fill buffer with known "dirty" values
            // Simulate a snake that moved and left data behind
            ushort[] dirtyPattern = [0xAA, 0xBB, 0xCC];
            snake.Initialize(new("dirty", 100, dirtyPattern));

            // Pre-check
            That(snake.Length, Is.EqualTo(3));
            That(snake.Head, Is.EqualTo(0xAA)); // Assuming insertion order

            // Act
            system.Initialize();

            // Assert
            // 1. Logical state MUST be reset
            That(snake.Length, Is.Zero, "Length not reset.");

            // 2. Physical memory MUST remain dirty (Performance Optimization)
            // Access raw memory via exposed Queue or unsafe accessor
            ref var queue = ref GetQueue(ref snake);
            var bufferSpan = queue.Buffer;

            // Verify bytes were not zeroed
            // If Initialize() called buffer.Clear(), this test would fail (violating speed requirement)
            var hasDirtyBytes = false;
            foreach (var val in bufferSpan)
            {
                if (val == 0) continue;
                hasDirtyBytes = true;
                break;
            }

            That(hasDirtyBytes, Is.True,
                "PERFORMANCE WARNING: Initialize() is clearing the Queue Buffer. " +
                "It should only reset indices (Head/Tail/Length) to be O(1).");
        }
    }
}