using Thanos.War.Structures;
using static NUnit.Framework.Assert;

namespace Thanos.Tests.Integration.CircularQueue;

public partial class CircularQueueTests
{
    [TestCaseSource(nameof(Capacities))]
    public void Enqueue_WhenQueueWrapsAround_ShouldPreserveFifoOrder(ushort capacity, int bufferSize)
    {
        // Skip capacities too small for a snake of length 3
        if (capacity < 4) Ignore("Capacity too small for simulation.");

        // Arrange
        var state = new CircularQueueState();
        var memory = new byte[bufferSize];
        var queue = new War.Structures.CircularQueue(memory, ref state, capacity);

        const int SnakeLength = 3;

        // 1. Setup Initial Body: [0, 1, 2]
        foreach (ushort index in Enumerable.Range(0, SnakeLength)) queue.Enqueue(index);

        // Act & Assert (Simulation Loop)
        var iterations = GetIterations(capacity);

        for (ushort i = 0; i < iterations; i++)
        {
            // Expected values follow a strict deterministic sequence
            var nextHeadValue = (ushort)(SnakeLength + i);
            var expectedDeqValue = i;

            // --- ACT: The Move ---
            queue.Enqueue(nextHeadValue);
            var removed = queue.Dequeue();

            // --- ASSERT: Invariants ---
            // 1. FIFO Integrity
            That(removed, Is.EqualTo(expectedDeqValue), $"Iter {i}: FIFO violation. Wrong value dequeued.");

            // 2. Structural Integrity
            That(queue.Length, Is.EqualTo(SnakeLength), $"Iter {i}: Length corrupted.");

            // 3. Pointers Integrity (Oracle Verification)
            // Head is always the last inserted value.
            // Tail is the next value to be dequeued (i + 1).
            // Neck (ElementBeforeTail) is i + 2.

            var expectedHead = nextHeadValue;
            var expectedTail = (ushort)(i + 1);
            var expectedNeck = (ushort)(i + 2);

            That(queue.PeekHead, Is.EqualTo(expectedHead), $"Iter {i}: Head pointer mismatch.");
            That(queue.PeekTail, Is.EqualTo(expectedTail), $"Iter {i}: Tail pointer mismatch.");
            That(queue.PeekElementBeforeTail, Is.EqualTo(expectedNeck), $"Iter {i}: Neck pointer mismatch.");
        }
    }
}