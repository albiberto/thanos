using System.Runtime.InteropServices;
using Thanos.War.Structures;
using static NUnit.Framework.Assert;

namespace Thanos.Tests.Integration.CircularQueue;

public partial class CircularQueueTests
{
    [TestCaseSource(nameof(Capacities))]
    public void EnqueueDequeue_WhenQueueWrapsAround_ShouldPreserveFifoOrder(ushort capacity, int bufferSize)
    {
        // Guard: Capacities smaller than the snake length (3) cannot support this specific scenario
        // without overlapping/overwriting immediately.
        if (capacity < 4)
        {
            Pass($"Skipping scenario for Capacity {capacity} (Too small for Len 3 simulation).");
            return;
        }

        // Arrange
        var state = new CircularQueueState();
        var memory = new byte[bufferSize];
        var queue = new War.Structures.CircularQueue(memory, ref state, capacity);

        const int SnakeLength = 3;

        // 1. Setup Initial Body: [0, 1, 2]
        foreach (ushort index in Enumerable.Range(0, SnakeLength))
            queue.Enqueue(index);

        // Act & Assert (Simulation Loop)
        var iterations = GetIterations(capacity);

        for (ushort i = 0; i < iterations; i++)
        {
            // Expected values follow a strict deterministic sequence
            var nextHeadValue = (ushort)(SnakeLength + i);
            var expectedDeqValue = i;

            // --- ACT: The Move (Slide Window) ---
            queue.Enqueue(nextHeadValue);
            var removed = queue.Dequeue();

            // --- ASSERT: Invariants ---

            // 1. FIFO Integrity (Value Check)
            That(removed, Is.EqualTo(expectedDeqValue), $"Iter {i}: FIFO violation. Wrong value dequeued.");

            // 2. Structural Integrity (Length Check)
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

    [TestCaseSource(nameof(Capacities))]
    public void PeekElementBeforeTail_WhenQueueIsTooShort_ShouldReturnRawMemoryLookahead(ushort capacity, int bufferSize)
    {
        // Scenario: Boundary Access.
        // ElementBeforeTail looks at (Tail + 1). If Length < 2, it looks at "future" or "old" memory.

        // Arrange
        var state = new CircularQueueState();
        var memory = new byte[bufferSize];
        var queue = new War.Structures.CircularQueue(memory, ref state, capacity);

        // Setup: Enqueue 1 element. Tail is at 0.
        // Head advances to 1.
        queue.Enqueue(0xAAAA);

        // Manually write to buffer at Tail + 1 (index 1)
        // to verify the property reads EXACTLY that memory cell.
        var span = MemoryMarshal.Cast<byte, ushort>(new Span<byte>(memory));
        span[1] = 0xBBBB;

        // Act
        // Queue has Length 1, so "ElementBeforeTail" logically doesn't exist.
        // But physically it does, and we expect it to be read.
        var phantomNeck = queue.PeekElementBeforeTail;

        // Assert
        That(phantomNeck, Is.EqualTo(0xBBBB), "Should read raw memory at (Tail + 1) regardless of logic.");
    }
}