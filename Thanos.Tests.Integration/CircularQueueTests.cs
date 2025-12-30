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
            yield return Case(2);
            yield return Case(4);
            yield return Case(8);

            // STANDARD LEAGUE: Small (7x7 -> next PO2: 64)
            yield return Case(64);

            // STANDARD LEAGUE: Medium (11x11 -> next PO2: 128)
            yield return Case(128);

            // HARD LIMIT: 256 (Byte index limit)
            yield return Case(256);
            yield break;

            static TestCaseData Case(ushort cap) => new(cap, cap * sizeof(ushort));
        }
    }

    [TestCaseSource(nameof(Capacities))]
    public void Constructor_WhenInitialized_ShouldHaveZeroLength(ushort capacity, int bufferSize)
    {
        // Arrange
        var state = new CircularQueueState();
        var memory = new byte[bufferSize];

        // Act
        var queue = new CircularQueue(memory, ref state, capacity);

        // Assert
        That(queue.Length, Is.Zero, "Initial length must be 0.");
        That(state.HeadIndex, Is.Zero, "Head index must start at 0.");
        That(state.TailIndex, Is.Zero, "Tail index must start at 0.");
    }

    [TestCaseSource(nameof(Capacities))]
    public void Clear_WhenQueueIsDirty_ShouldResetIndicesAndLength(ushort capacity, int bufferSize)
    {
        // Arrange
        var state = new CircularQueueState();
        var memory = new byte[bufferSize];
        var queue = new CircularQueue(memory, ref state, capacity);

        // Setup: Dirty the state (simulate game turns)
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