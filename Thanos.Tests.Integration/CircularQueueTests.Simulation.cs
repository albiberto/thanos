using Thanos.War.Structures;
using static NUnit.Framework.Assert;

namespace Thanos.Tests.Integration;

public partial class CircularQueueTests
{
    private const int WrapCycles = 50;

    private static int GetIterations(ushort capacity) => capacity * WrapCycles;

    [TestCaseSource(nameof(Capacities))]
    public void Movement_ShouldPreserveOrder_ThroughMultipleWrapAround(ushort capacity, int bufferSize)
    {
        // Skip capacities too small for a snake of length 3
        if (capacity < 4) Ignore("Capacity too small for simulation.");

        var state = new CircularQueueState();
        var memory = new byte[bufferSize];
        var queue = new CircularQueue(memory, ref state, capacity);

        const int SnakeLength = 3;

        // 1. Setup Initial Body: [0, 1, 2]
        // Avoid filling the entire capacity to prevent immediate overwrite 
        // on the very first Enqueue of the simulation loop.
        foreach (ushort index in Enumerable.Range(0, SnakeLength)) queue.Enqueue(index);

        // 2. Simulation Loop
        var iterations = GetIterations(capacity);
        for (ushort i = 0; i < iterations; i++)
        {
            // Expected values follow a strict deterministic sequence
            var head = (ushort)(SnakeLength + i);
            var valueToRemove = i;

            // --- ACT: The Move ---
            queue.Enqueue(head);
            var removed = queue.Dequeue();

            // --- ASSERT: Invariants ---

            // 1. FIFO Integrity
            That(removed, Is.EqualTo(valueToRemove), $"Iter {i}: FIFO violation. Wrong value dequeued.");

            // 2. Structural Integrity
            That(queue.Length, Is.EqualTo(SnakeLength), $"Iter {i}: Length corrupted.");

            // 3. Pointers Integrity
            var expectedHead = head;
            var expectedTail = (ushort)(i + 1);
            var expectedNeck = (ushort)(i + 2);

            That(queue.PeekHead, Is.EqualTo(expectedHead), $"Iter {i}: Head pointer mismatch.");
            That(queue.PeekTail, Is.EqualTo(expectedTail), $"Iter {i}: Tail pointer mismatch.");
            That(queue.PeekElementBeforeTail, Is.EqualTo(expectedNeck), $"Iter {i}: Neck pointer mismatch.");
        }
    }

    [Test]
    public void Growth_Should_SaturateAt255_And_Overwrite()
    {
        const int capacity = 256;
        const int bufferSize = 256 * sizeof(ushort);
        const int physicalLength = capacity - 1; // 255

        var state = new CircularQueueState();
        var memory = new byte[bufferSize];
        var queue = new CircularQueue(memory, ref state, capacity);

        // --- PHASE 1: Fill up to the Byte Limit (255) ---
        foreach (var i in Enumerable.Range(0, physicalLength)) queue.Enqueue((ushort)i);

        // Verify Pre-Saturation state
        That(queue.Length, Is.EqualTo(physicalLength), "Length should be exactly 255.");
        That(queue.PeekHead, Is.EqualTo(254), "Head should point to 254.");

        // --- PHASE 2: Massive Overwrite (Constant Testing) ---
        var extraIterations = GetIterations(capacity);

        foreach (var i in Enumerable.Range(0, extraIterations))
        {
            var val = (ushort)(255 + i);
            queue.Enqueue(val);

            // Determine expected values in fixed slots 0 (Tail) and 1 (BeforeTail) 
            // using simple cycle counts and remainders.
            var cycle = val / capacity; // Full buffer fills count
            var head = val % capacity; // Current Head position index

            // 1. Logical Saturation
            That(queue.Length, Is.EqualTo(physicalLength), $"Length failed at i={i}");

            // 2. Head Integrity
            That(queue.PeekHead, Is.EqualTo(val), $"Head integrity failed at i={i}");

            // 3. Tail Integrity (Buffer[0])
            // Buffer[0] is updated only when head wraps to 0.
            var expectedTail = (ushort)(cycle * capacity);
            That(queue.PeekTail, Is.EqualTo(expectedTail), $"Tail integrity failed at i={i}");

            // 4. Before Tail Integrity (Buffer[1])
            // Buffer[1] is updated when head reaches 1. 
            // If current head < 1, it holds value from previous cycle.
            var expectedBeforeTail = head >= 1 ? (ushort)(cycle * capacity + 1) : (ushort)((cycle - 1) * capacity + 1);
            That(queue.PeekElementBeforeTail, Is.EqualTo(expectedBeforeTail), $"BeforeTail integrity failed at i={i}");
        }
    }
}