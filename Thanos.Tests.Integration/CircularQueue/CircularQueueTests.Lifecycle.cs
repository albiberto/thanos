using Thanos.War.Structures;
using static NUnit.Framework.Assert;

namespace Thanos.Tests.Integration.CircularQueue;

public partial class CircularQueueTests
{
    [TestCaseSource(nameof(Capacities))]
    public void Constructor_WhenInitialized_ShouldHaveZeroLengthAndResetIndices(ushort capacity, int bufferSize)
    {
        // Arrange
        var state = new CircularQueueState();
        var memory = new byte[bufferSize];

        // Act
        var queue = new War.Structures.CircularQueue(memory, ref state, capacity);

        // Assert
        That(queue.Length, Is.Zero, "Initial length must be 0.");

        // Direct State Inspection (White-box testing required for struct layout verification)
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

        // Pre-Assert to validate setup validity
        That(queue.Length, Is.Not.Zero, "Setup failed: Queue should be dirty.");

        // Act
        queue.Clear();

        // Assert
        That(queue.Length, Is.Zero, "Length must be reset to 0.");
        That(state.HeadIndex, Is.Zero, "Head index must be reset to 0.");
        That(state.TailIndex, Is.Zero, "Tail index must be reset to 0.");
    }
    
    [TestCaseSource(nameof(Capacities))]
    public void State_WhenReattachedToDirtyMemory_ShouldResumeOperationsCorrectly(ushort capacity, int bufferSize)
    {
        // Scenario: Arena Reuse. 
        // Verifichiamo che una nuova "View" (struct) su memoria esistente erediti correttamente lo stato.

        // Arrange
        var state = new CircularQueueState();
        var memory = new byte[bufferSize];

        // Fase 1: Popolazione iniziale con la "Vista A"
        {
            var viewA = new War.Structures.CircularQueue(memory, ref state, capacity);
            viewA.Enqueue(10);
            viewA.Enqueue(20);
            viewA.Dequeue(); // Rimuove 10, Tail avanza
        }

        // Assert Intermedio: Lo stato deve essere persistito fuori dallo scope della struct
        That(state.Length, Is.EqualTo(1));
        That(state.TailIndex, Is.EqualTo(1));

        // Act: Fase 2 - Creazione "Vista B" sugli stessi dati
        var viewB = new War.Structures.CircularQueue(memory, ref state, capacity);

        // Verifica che la Vista B veda i dati corretti
        That(viewB.PeekHead, Is.EqualTo(20), "Rehydrated view reading wrong Head.");
        
        // Operazione sulla Vista B
        viewB.Enqueue(30);

        // Assert
        // Verifichiamo continuità FIFO tra le due sessioni
        var val1 = viewB.Dequeue(); // Deve essere 20 (residuo Vista A)
        var val2 = viewB.Dequeue(); // Deve essere 30 (nuovo Vista B)

        That(val1, Is.EqualTo(20), "FIFO continuity broken across views.");
        That(val2, Is.EqualTo(30));
    }
}