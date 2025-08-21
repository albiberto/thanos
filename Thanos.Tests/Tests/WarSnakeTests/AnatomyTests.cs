using Thanos.War.Snake;

namespace Thanos.Tests.Tests.WarSnakeTests;

/// <summary>
/// Contains unit tests for the Anatomy struct.
/// This entire fixture is run for each capacity defined in the Capacities source.
/// </summary>
[TestFixtureSource(nameof(Capacities))]
public class AnatomyTests(int capacity)
{
    // L'elenco delle capacità rimane lo stesso
    public static int[] Capacities { get; } = [4, 8, 16, 32, 64, 128, 256, 512, 1024];

    // I campi non sono più 'readonly' perché vengono inizializzati nel SetUp,
    // non nel costruttore.
    private ushort _defaultHead;
    private ushort _defaultTail;
    private int _defaultLength;
    private int _defaultTailIndex;

    /// <summary>
    /// Questo metodo viene eseguito una sola volta per ogni valore di 'capacity'.
    /// Imposta i valori di default dinamici che verranno usati da tutti i test
    /// in questa specifica istanza della fixture.
    /// </summary>
    [OneTimeSetUp]
    public void PrepareDefaultValues()
    {
        // La logica di inizializzazione è ora qui, separata dal costruttore.
        _defaultLength = capacity / 2;
        _defaultHead = (ushort)(_defaultLength > 0 ? _defaultLength - 1 : 0);
        _defaultTail = 0;
        _defaultTailIndex = 0;
    }
    
    [Test(Description = "Ensures the constructor correctly assigns all initial values.")]
    public void Constructor_ShouldInitializeAllPropertiesCorrectly()
    {
        // Arrange
        var nextHeadIndex = _defaultLength > 0 ? _defaultLength : 0;

        // Act
        var anatomy = new Anatomy(_defaultHead, _defaultTail, capacity, _defaultLength, nextHeadIndex, _defaultTailIndex);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(anatomy.Head, Is.EqualTo(_defaultHead));
            Assert.That(anatomy.NextHeadIndex, Is.EqualTo(nextHeadIndex));
            Assert.That(anatomy.Tail, Is.EqualTo(_defaultTail));
            Assert.That(anatomy.TailIndex, Is.EqualTo(_defaultTailIndex));
            Assert.That(anatomy.Length, Is.EqualTo(_defaultLength));
            Assert.That(anatomy.Capacity, Is.EqualTo(capacity));
        });
    }

// --- PushHead Tests ---

    [Test(Description = "Tests PushHead from the start of the buffer.")]
    public void PushHead_WhenAtIndexZero_ShouldIncrementNextHeadIndex()
    {
        var anatomy = new Anatomy(0, 0, capacity, 1, 0, 0);
        anatomy.PushHead(120);
        Assert.That(anatomy.NextHeadIndex, Is.EqualTo(1));
    }

    [Test(Description = "Tests PushHead from a dynamically calculated middle index.")]
    public void PushHead_WhenInMiddleOfBuffer_ShouldIncrementNextHeadIndex()
    {
        var middleIndex = capacity / 2;
        var anatomy = new Anatomy(0, 0, capacity, 1, middleIndex, 0);
        anatomy.PushHead(120);
        Assert.That(anatomy.NextHeadIndex, Is.EqualTo(middleIndex + 1));
    }

    [Test(Description = "Tests the circular buffer wrap-around for NextHeadIndex.")]
    public void PushHead_WhenAtBufferEnd_ShouldWrapNextHeadIndexToZero()
    {
        var initialNextHeadIndex = capacity - 1;
        var anatomy = new Anatomy(0, 0, capacity, 1, initialNextHeadIndex, 0);
        anatomy.PushHead(120);
        Assert.That(anatomy.NextHeadIndex, Is.EqualTo(0));
    }
    
    // --- PopTail Tests (ora coerenti con PushHead) ---
    
    [Test(Description = "Tests PopTail from the start of the buffer.")]
    public void PopTail_WhenAtIndexZero_ShouldIncrementTailIndex()
    {
        // Arrange
        var anatomy = new Anatomy(0, 0, capacity, 1, 0, 0);
        
        // Act
        anatomy.PopTail();

        // Assert
        Assert.That(anatomy.TailIndex, Is.EqualTo(1));
    }
    
    [Test(Description = "Tests PopTail from a dynamically calculated middle index.")]
    public void PopTail_WhenInMiddleOfBuffer_ShouldIncrementTailIndex()
    {
        // Arrange
        var middleIndex = capacity / 2;
        var anatomy = new Anatomy(0, 0, capacity, 1, 0, middleIndex);
        
        // Act
        anatomy.PopTail();

        // Assert
        Assert.That(anatomy.TailIndex, Is.EqualTo(middleIndex + 1));
    }

    [Test(Description = "Tests the circular buffer wrap-around for TailIndex.")]
    public void PopTail_WhenAtBufferEnd_ShouldWrapTailIndexToZero()
    {
        // Arrange
        var initialTailIndex = capacity - 1;
        var anatomy = new Anatomy(0, 0, capacity, 1, 0, initialTailIndex);

        // Act
        anatomy.PopTail();

        // Assert
        Assert.That(anatomy.TailIndex, Is.EqualTo(0));
    }


    // --- IncrementLength Tests ---

    [Test]
    public void IncrementLength_WhenBelowCapacity_ShouldIncrementLengthByOne()
    {
        // Arrange
        var anatomy = new Anatomy(_defaultHead, _defaultTail, capacity, _defaultLength, 0, _defaultTailIndex);
        
        if (_defaultLength == capacity) 
            Assert.Pass("Scenario non applicabile per questa capacità.");

        // Act
        anatomy.IncrementLength();

        // Assert
        Assert.That(anatomy.Length, Is.EqualTo(_defaultLength + 1));
    }

    [Test]
    public void IncrementLength_WhenAtCapacity_ShouldNotChangeLength()
    {
        // Arrange: Create an anatomy where length is already at maximum capacity.
        var anatomy = new Anatomy(_defaultHead, _defaultTail, capacity, capacity, 0, _defaultTailIndex);

        // Act
        anatomy.IncrementLength();

        // Assert
        Assert.That(anatomy.Length, Is.EqualTo(capacity));
    }
}