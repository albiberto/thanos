using Thanos.War.Structures;

namespace Thanos.Tests.Integration.CircularQueue;

/// <summary>
///     Integration tests for the <see cref="CircularQueue" /> structure.
///     Validates memory wrapping logic, state management, and FIFO behavior
///     crucial for the Snake body representation.
/// </summary>
[TestFixture]
[Parallelizable(ParallelScope.All)]
public partial class CircularQueueTests
{
    private const int WrapCycles = 50;

    // Calculates total iterations to force multiple wrap-arounds
    private static int GetIterations(ushort capacity) => capacity * WrapCycles;

    /// <summary>
    ///     Generates test cases for various power-of-two capacities.
    ///     Includes the pre-calculated byte buffer size to simplify test setup.
    /// </summary>
    public static IEnumerable<TestCaseData> Capacities
    {
        get
        {
            // EDGE CASES: Extreme Constriction (Minimizes masking logic risks)
            yield return Case(2);
            yield return Case(4);
            yield return Case(8);

            // STANDARD LEAGUE: Small (7x7 -> next PO2: 64)
            yield return Case(64);

            // STANDARD LEAGUE: Medium (11x11 -> next PO2: 128)
            yield return Case(128);

            // HARD LIMIT: 256 (Byte index limit in CircularQueueState)
            yield return Case(256);
        }
    }

    private static TestCaseData Case(ushort cap) =>
        new TestCaseData(cap, cap * sizeof(ushort))
            .SetName($"Capacity_{cap}");
}