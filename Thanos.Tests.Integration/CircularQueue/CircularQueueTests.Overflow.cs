using Thanos.War.Structures;
using static NUnit.Framework.Assert;

namespace Thanos.Tests.Integration.CircularQueue;

public partial class CircularQueueTests
{
    [Test]
    public void Enqueue_WhenCapacityIsReached_ShouldOverwriteOldestElement_WithoutExpandingLength()
    {
        // Scenario: Hard saturation (255 elements).
        // This tests the explicit byte-limit check in Enqueue:
        // if (_state.Length < byte.MaxValue) _state.Length++;

        const int capacity = 256;
        const int bufferSize = 256 * sizeof(ushort);
        const int physicalLength = capacity - 1; // 255 (Max byte value constraint)

        // Arrange
        var state = new CircularQueueState();
        var memory = new byte[bufferSize];
        var queue = new War.Structures.CircularQueue(memory, ref state, capacity);

        // --- PHASE 1: Fill up to the Byte Limit (255) ---
        foreach (var i in Enumerable.Range(0, physicalLength))
            queue.Enqueue((ushort)i);

        // Verify Pre-Saturation state
        That(queue.Length, Is.EqualTo(physicalLength), "Length should be exactly 255 (Byte MaxValue saturation).");
        That(queue.PeekHead, Is.EqualTo(254), "Head should point to 254.");

        // --- PHASE 2: Massive Overwrite (Stress Test) ---
        var extraIterations = GetIterations(capacity);

        for (var i = 0; i < extraIterations; i++)
        {
            var val = (ushort)(255 + i);

            // Act: Enqueue WITHOUT Dequeue (Force Saturation)
            queue.Enqueue(val);

            // Assert
            // We verify the internal buffer state directly using math logic (Modulo arithmetic)

            var cycle = val / capacity; // How many times we filled the buffer

            // 1. Logical Saturation check
            That(queue.Length, Is.EqualTo(physicalLength), $"Length exceeded Byte.MaxValue limit at i={i}");

            // 2. Head Integrity
            That(queue.PeekHead, Is.EqualTo(val), $"Head integrity failed at i={i}");

            // 3. Tail Integrity (Underlying Buffer Check)
            // Since we are NOT calling Dequeue(), the Tail Index logic in the struct stays at 0.
            // However, the *data* at Buffer[0] (where tail points) gets overwritten 
            // when Head wraps around.
            // 
            // This confirms that CircularQueue behaves as a ring buffer where 
            // old data is destructively overwritten if not dequeued.

            var expectedTailValue = (ushort)(cycle * capacity);
            That(queue.PeekTail, Is.EqualTo(expectedTailValue), $"Tail data integrity failed at i={i}. Ring buffer overwrite logic broken.");
        }
    }

    [TestCaseSource(nameof(Capacities))]
    public void Dequeue_WhenEmpty_ShouldUnderflowLengthToMaxByte(ushort capacity, int bufferSize)
    {
        // Scenario: Performance over Safety.
        // Verify that lack of checks causes deterministic underflow (0 -> 255)
        // instead of exceptions or undefined behavior.

        // Arrange
        var state = new CircularQueueState();
        var memory = new byte[bufferSize];
        var queue = new War.Structures.CircularQueue(memory, ref state, capacity);

        // Act
        var result = queue.Dequeue(); // Empty queue!

        // Assert
        // 1. Result is garbage (0 default)
        That(result, Is.Zero);

        // 2. State is corrupted specifically via Underflow
        That(state.Length, Is.EqualTo(byte.MaxValue), "Length must underflow to 255.");

        // 3. Tail advances anyway (destroying alignment, as expected)
        That(state.TailIndex, Is.EqualTo(1), "Tail index must advance unconditionally.");
    }
}