using Thanos.War.Structures;
using static NUnit.Framework.Assert;

namespace Thanos.Tests.Integration.CircularQueue;

public partial class CircularQueueTests
{
    [Test]
    public void Enqueue_WhenCapacityIsReached_ShouldOverwriteOldestElement()
    {
        // Scenario: Hard saturation (255 elements).
        const int capacity = 256;
        const int bufferSize = 256 * sizeof(ushort);
        const int physicalLength = capacity - 1; // 255 (Max byte value constraint)

        // Arrange
        var state = new CircularQueueState();
        var memory = new byte[bufferSize];
        var queue = new War.Structures.CircularQueue(memory, ref state, capacity);

        // --- PHASE 1: Fill up to the Byte Limit (255) ---
        foreach (var i in Enumerable.Range(0, physicalLength)) queue.Enqueue((ushort)i);

        // Verify Pre-Saturation state
        That(queue.Length, Is.EqualTo(physicalLength), "Length should be exactly 255.");
        That(queue.PeekHead, Is.EqualTo(254), "Head should point to 254.");

        // --- PHASE 2: Massive Overwrite (Stress Test) ---
        var extraIterations = GetIterations(capacity);

        for (var i = 0; i < extraIterations; i++)
        {
            var val = (ushort)(255 + i);

            // Act
            queue.Enqueue(val);

            // Assert
            // We verify the internal buffer state directly using math logic (Modulo arithmetic)

            var cycle = val / capacity; // How many times we filled the buffer
            var headIdx = val % capacity; // Current Head physical index

            // 1. Logical Saturation
            That(queue.Length, Is.EqualTo(physicalLength), $"Length failed at i={i}");

            // 2. Head Integrity
            That(queue.PeekHead, Is.EqualTo(val), $"Head integrity failed at i={i}");

            // 3. Tail Integrity (Buffer[0])
            // Buffer[0] is updated only when head wraps to 0.
            var expectedTail = (ushort)(cycle * capacity);
            That(queue.PeekTail, Is.EqualTo(expectedTail), $"Tail integrity failed at i={i}");
        }
    }
}