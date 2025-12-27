using Thanos.War.Structures;
using static NUnit.Framework.Assert;

namespace Thanos.Tests.Integration;

[TestFixture]
public class CircularQueueStateTests
{
    // Setup dei dati di test
    public static IEnumerable<TestCaseData> TestCapacities =>
    [
        new((ushort)2), 
        new((ushort)4), 
        new((ushort)8), 
        new((ushort)16), 
        new((ushort)32), 
        new((ushort)64), 
        new((ushort)128), 
        new((ushort)256)
    ];

    // TEST 1: Configurazione (Logic)
    [TestCaseSource(nameof(TestCapacities))]
    public void PlacementNew_WhenCapacityIsPowerOfTwo_ThenCalculatesCorrectMask(ushort capacity)
    {
        var state = new CircularQueueState();
        var expectedMask = (ushort)(capacity - 1);

        state.PlacementNew(capacity);

        That(state.WrapMask, Is.EqualTo(expectedMask),"WrapMask must be (Capacity - 1) for bitwise operations.");
    }

    // TEST 2: Simulazione Movimento e Wrapping (Simulation/Invariants)
    // Sostituisce tutti i test manuali di Advance/Wrap
    [TestCaseSource(nameof(TestCapacities))]
    public void Indices_WhenSubjectToSlidingWindowStress_ThenWrapCorrectly(ushort capacity)
    {
        var state = new CircularQueueState();
        state.PlacementNew(capacity);

        var iterations = capacity * 2; // Garantisce almeno 2 wrap completi

        for (var i = 0; i < iterations; i++)
        {
            // Act 1: Enqueue
            state.AdvanceHead();

            // Assert 1: Head logic
            var expectedHead = (ushort)((i + 1) % capacity);
            Multiple(() =>
            {
                That(state.HeadIndex, Is.EqualTo(expectedHead), $"Iter {i}: Head mismatch");
                That(state.Length, Is.EqualTo(1), $"Iter {i}: Length mismatch after enqueue");
            });

            // Act 2: Dequeue
            state.AdvanceTail();

            // Assert 2: Tail logic
            var expectedTail = (ushort)((i + 1) % capacity);
            Multiple(() =>
            {
                That(state.TailIndex, Is.EqualTo(expectedTail), $"Iter {i}: Tail mismatch");
                That(state.Length, Is.EqualTo(0), $"Iter {i}: Length mismatch after dequeue");
            });
        }
    }

    // TEST 3: Reset dello Stato (Logic)
    [TestCaseSource(nameof(TestCapacities))]
    public void Reset_WhenStateIsDirty_ThenResetsAllIndicesAndLength(ushort capacity)
    {
        var state = new CircularQueueState();
        state.PlacementNew(capacity);
        
        // Sporchiamo lo stato
        state.AdvanceHead();
        state.AdvanceHead();
        state.AdvanceTail();

        state.Reset();

        Multiple(() =>
        {
            That(state.Length, Is.Zero, "Length should be 0");
            That(state.HeadIndex, Is.Zero, "HeadIndex should be 0");
            That(state.TailIndex, Is.Zero, "TailIndex should be 0");
        });
    }
}