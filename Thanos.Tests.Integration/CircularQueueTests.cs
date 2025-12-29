using Thanos.War.Structures;
using static NUnit.Framework.Assert;

namespace Thanos.Tests.Integration;

[TestFixture]
public class CircularQueueIntegrationTests
{
    // Testiamo le capacità critiche: 
    // 128 (Target 11x11)
    // 2, 4 (Edge cases piccolissimi per stressare il wrap immediato)
    public static IEnumerable<TestCaseData> Capacities =>
    [
        new((ushort)2),
        new((ushort)4),
        new((ushort)8),
        new((ushort)16),
        new((ushort)32),
        new((ushort)64),
        new((ushort)128)
    ];

    // TEST 1: Inizializzazione Pulita
    [TestCaseSource(nameof(Capacities))]
    public void Initialize_ShouldStartEmpty(ushort capacity)
    {
        var state = new CircularQueueState();
        var memory = new byte[capacity * sizeof(ushort)];
        var queue = new CircularQueue(memory, ref state, capacity);

        That(queue.Length, Is.Zero, "Length iniziale deve essere 0");
        That(state.HeadIndex, Is.Zero, "HeadIndex deve essere 0");
        That(state.TailIndex, Is.Zero, "TailIndex deve essere 0");
    }

    // TEST 2: Reset
    [TestCaseSource(nameof(Capacities))]
    public void Reset_ShouldCleanState_AfterUsage(ushort capacity)
    {
        var state = new CircularQueueState();
        var memory = new byte[capacity * sizeof(ushort)];
        var queue = new CircularQueue(memory, ref state, capacity);

        // Sporchiamo lo stato
        queue.Enqueue(123);
        queue.Enqueue(456);
        queue.Dequeue();

        // Reset
        queue.Clear();

        That(queue.Length, Is.Zero, "Length deve tornare a 0");
        That(state.HeadIndex, Is.Zero, "HeadIndex deve tornare a 0");
        That(state.TailIndex, Is.Zero, "TailIndex deve tornare a 0");
    }

    // TEST 3: THE BATTLESNAKE SIMULATION (Logic + Stress)
    // Simula un serpente che si muove all'infinito nel buffer.
    // Verifica Head, Tail, ElementBeforeTail e Length ad ogni singolo passo.
    [TestCaseSource(nameof(Capacities))]
    public void SimulateSnakeMovement_StressTest_Wrapping(ushort capacity)
    {
        if (capacity < 4) Ignore("Capacity troppo piccola per un serpente di lunghezza 3");

        var state = new CircularQueueState();
        var memory = new byte[capacity * sizeof(ushort)];
        var queue = new CircularQueue(memory, ref state, capacity);

        const int snakeLength = 3;

        // Setup iniziale: Creiamo il serpente di lunghezza 3
        // Valori nel buffer: [10, 20, 30]
        queue.Enqueue(10);
        queue.Enqueue(20);
        queue.Enqueue(30);

        // Iniziamo il loop di movimento
        // 10 giri completi per essere sicuri al 100% che l'overflow dei byte index non rompa nulla
        var iterations = capacity * 10;

        // Valore corrente della testa (inizia da 30, quindi il prossimo sarà 40)
        var nextHeadValue = 40;
        // Valore atteso della coda (inizia da 10)
        var expectedTailValue = 10;

        for (var i = 0; i < iterations; i++)
        {
            // --- FASE 1: ENQUEUE (Nuova Testa) ---
            queue.Enqueue((ushort)nextHeadValue);

            // Verifica post-enqueue (Il serpente è momentaneamente lungo 4)
            That(queue.Length, Is.EqualTo(snakeLength + 1), $"Iter {i}: Length errata dopo Enqueue");
            That(queue.PeekHead, Is.EqualTo(nextHeadValue), $"Iter {i}: PeekHead errato");

            // --- FASE 2: DEQUEUE (Rimozione Vecchia Coda) ---
            // In Battlesnake questo avviene se non mangiamo cibo
            var removedTail = queue.Dequeue();

            // Calcoliamo cosa ci aspettiamo come "ElementBeforeTail"
            // Se coda era 10, e passo è 10, il prossimo elemento è 20.
            var expectedElementBeforeTail = expectedTailValue + 10;

            // --- FASE 3: VERIFICA COMPLETA DELLO STATO (Invariant Check) ---
            // 1. Verifica valore rimosso
            That(removedTail, Is.EqualTo(expectedTailValue), $"Iter {i}: Dequeue ha restituito il valore errato");

            // 2. Verifica Lunghezza (Tornata a 3)
            That(queue.Length, Is.EqualTo(snakeLength), $"Iter {i}: Length errata dopo Dequeue");

            // 3. Verifica Head (Deve essere ancora quella appena inserita)
            That(queue.PeekHead, Is.EqualTo(nextHeadValue), $"Iter {i}: PeekHead corrotto dopo Dequeue");

            // 4. Verifica Tail (La nuova coda deve essere il valore successivo: 20, 30...)
            That(queue.PeekTail, Is.EqualTo(expectedTailValue + 10), $"Iter {i}: PeekTail errato (Indice Tail non avanzato correttamente?)");

            // 5. Verifica ElementBeforeTail (Cruciale per collisioni collo)
            // Se Tail è 20, BeforeTail deve essere 30.
            That(queue.PeekElementBeforeTail, Is.EqualTo(expectedElementBeforeTail + 10), $"Iter {i}: PeekElementBeforeTail errato");

            // Prepariamo i valori per il prossimo giro
            nextHeadValue += 10;
            expectedTailValue += 10;
        }
    }
}