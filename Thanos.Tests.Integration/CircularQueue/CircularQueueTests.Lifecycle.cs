using Thanos.War.Structures;
using static NUnit.Framework.Assert;

namespace Thanos.Tests.Integration.CircularQueue;

public partial class CircularQueueTests
{
    [TestCaseSource(nameof(Capacities))]
    public void Constructor_WhenInitialized_ShouldHaveZeroLength(ushort capacity, int bufferSize)
    {
        // Arrange
        var state = new CircularQueueState();
        var memory = new byte[bufferSize];

        // Act
        var queue = new War.Structures.CircularQueue(memory, ref state, capacity);

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
        var queue = new War.Structures.CircularQueue(memory, ref state, capacity);

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