using Thanos.War.Structures;
using static NUnit.Framework.Assert;

namespace Thanos.Tests.Integration;

/// <summary>
///     Integration tests for the <see cref="CircularQueue" /> structure.
///     Validates memory wrapping logic, state management, and FIFO behavior
///     crucial for the Snake body representation.
/// </summary>
[TestFixture]
[Parallelizable(ParallelScope.All)]
public partial class CircularQueueTests
{
    /// <summary>
    ///     Generates test cases for various power-of-two capacities.
    ///     Includes the pre-calculated byte buffer size to simplify test setup.
    /// </summary>
    public static IEnumerable<TestCaseData> Capacities
    {
        get
        {
            // EDGE CASES: Extreme Constriction
            // Forces immediate wrapping to verify index math logic.
            yield return Case(2);
            yield return Case(4);
            yield return Case(8);
            yield return Case(16);
            yield return Case(32);

            // STANDARD LEAGUE: Small (7x7 = 49 cells)
            // Next Power of 2 is 64. Fully safe cover.
            yield return Case(64);

            // STANDARD LEAGUE: Medium (11x11 = 121 cells)
            // Next Power of 2 is 128. Fully safe cover.
            yield return Case(128);

            // HARD LIMIT: 256 (19x19 = 361 cells, capped)
            // Since we use 'byte' for internal indices, we cannot exceed a capacity of 256.
            // 256 represents the maximum range addressable by a single byte (0..255) before natural overflow.
            yield return Case(256);
            yield break;

            static TestCaseData Case(ushort cap) => new(cap, cap * sizeof(ushort));
        }
    }

    [TestCaseSource(nameof(Capacities))]
    public void Initialize_ShouldStartWithZeroLength_AndResetIndices(ushort capacity, int bufferSize)
    {
        // Arrange
        var state = new CircularQueueState();
        var memory = new byte[bufferSize]; // Size injected directly

        // Act
        var queue = new CircularQueue(memory, ref state, capacity);

        // Assert
        That(queue.Length, Is.Zero, "Initial length must be 0.");
        That(state.HeadIndex, Is.Zero, "Head index must start at 0.");
        That(state.TailIndex, Is.Zero, "Tail index must start at 0.");
    }

    [TestCaseSource(nameof(Capacities))]
    public void Clear_ShouldResetState_AfterUsage(ushort capacity, int bufferSize)
    {
        // Arrange
        var state = new CircularQueueState();
        var memory = new byte[bufferSize];
        var queue = new CircularQueue(memory, ref state, capacity);

        // Dirty the state (simulate game turns)
        queue.Enqueue(100);
        queue.Enqueue(200);
        queue.Dequeue();

        // Act
        queue.Clear();

        // Assert
        That(queue.Length, Is.Zero, "Length must be reset to 0.");
        That(state.HeadIndex, Is.Zero, "Head index must be reset to 0.");
        That(state.TailIndex, Is.Zero, "Tail index must be reset to 0.");
    }
}