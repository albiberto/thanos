using Thanos.SourceGen;
using static NUnit.Framework.Assert;

namespace Thanos.Tests.Integration.SnakeSystem;

public partial class SnakesSystemTests
{
    [TestCaseSource(nameof(SystemScenarios))]
    public void CopyFrom_WhenSourceHasComplexState_ShouldCloneToDestination(SnakesSystemTestContext sourceContext)
    {
        // Arrange: Create a matching destination context
        // We infer parameters from the source context to ensure compatibility
        using var destinationContext = new SnakesSystemTestContext(
            sourceContext.MapName,
            (ushort)(sourceContext.Layout.FoodBitboard.Count<ulong>() * 64), // Reverse engineering area from bitboard size approximation
            sourceContext.Layout.QueueCapacity,
            (byte)sourceContext.ActiveCount,
            (byte)sourceContext.LayoutCapacity
        );

        using (sourceContext) // Ensure source is disposed
        {
            var source = sourceContext.Build();
            var destination = destinationContext.Build();

            // Setup Source: Distinct states
            for (var i = 0; i < sourceContext.ActiveCount; i++)
            {
                var hp = (byte)(100 - i * 10);
                // Create a body segment to verify queue copy
                var body = new[] { (ushort)(i * 10), (ushort)(i * 10 + 1) };

                source[i].Initialize(new Snake($"s{i}", hp, body));
            }

            // Act
            destination.CopyFrom(in source);

            // Assert
            for (var i = 0; i < sourceContext.ActiveCount; i++)
            {
                var srcSnake = source[i];
                var dstSnake = destination[i];

                // Verify Vital Signs
                That(dstSnake.HP, Is.EqualTo(srcSnake.HP), $"Snake {i} HP copy failed.");
                That(dstSnake.Length, Is.EqualTo(srcSnake.Length), $"Snake {i} Length copy failed.");

                // Verify Queue State
                That(dstSnake.Head, Is.EqualTo(srcSnake.Head), $"Snake {i} Head copy failed.");
                That(dstSnake.Tail, Is.EqualTo(srcSnake.Tail), $"Snake {i} Tail copy failed.");

                // Verify Bitboard integrity (Hash/PopCount)
                That(dstSnake.Body.PopCount(), Is.EqualTo(srcSnake.Body.PopCount()), $"Snake {i} Bitboard PopCount mismatch.");
            }
        }
    }

    [TestCaseSource(nameof(SystemScenarios))]
    public void CopyFrom_WhenDestinationIsModifiedAfterCopy_ShouldNotAffectSource(SnakesSystemTestContext sourceContext)
    {
        // Scenario: Deep Copy verification

        using var destinationContext = new SnakesSystemTestContext(
            sourceContext.MapName,
            (ushort)(sourceContext.Layout.FoodBitboard.Count<ulong>() * 64),
            sourceContext.Layout.QueueCapacity,
            (byte)sourceContext.ActiveCount,
            (byte)sourceContext.LayoutCapacity
        );

        using (sourceContext)
        {
            var source = sourceContext.Build();
            var destination = destinationContext.Build();

            // Setup initial state
            source[0].Initialize(new Snake("hero", 100, [1, 2]));

            // Act
            destination.CopyFrom(in source);

            // Modify Destination
            // Use Indexer to get ref, then call method
            destination[0].UpdateAfterMove(3, false, 10); // Move head to 3, take damage

            // Assert
            // Destination Changed
            That(destination[0].Head, Is.EqualTo(3));
            That(destination[0].HP, Is.EqualTo(90));

            // Source Unchanged (Isolation)
            That(source[0].Head, Is.EqualTo(1), "Source was modified! Memory overlap detected.");
            That(source[0].HP, Is.EqualTo(100), "Source HP changed.");
        }
    }
}