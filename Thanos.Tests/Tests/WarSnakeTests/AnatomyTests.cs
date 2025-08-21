using Thanos.War.Snake;

namespace Thanos.Tests.Tests.WarSnakeTests;

[TestFixtureSource(nameof(Capacities))]
public class AnatomyTests(int capacity)
{
    public static int[] Capacities { get; } = [4, 8, 16, 32, 128, 256, 512, 1024];

    // --- Test del Costruttore ---

    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    [TestCase(int.MaxValue)]
    public void Constructor_WithDefaultTailIndex_ShouldInitializeStateCorrectly(int ratio)
    {
        // Arrange
        var length = ratio == int.MaxValue ? capacity : capacity / ratio;
        
        // Act
        var anatomy = new Anatomy(capacity, length);

        // Assert: Verifica lo stato completo dell'oggetto appena creato.
        Assert.Multiple(() =>
        {
            Assert.That(anatomy.Capacity, Is.EqualTo(capacity), "Capacity should be set correctly.");
            Assert.That(anatomy.Length, Is.EqualTo(length), "Length should be set correctly.");
            Assert.That(anatomy.TailIndex, Is.Zero, "Default TailIndex should be 0.");

            // Verifica le proprietà calcolate
            Assert.That(anatomy.CapacityMask, Is.EqualTo(capacity - 1), "CapacityMask should be set correctly.");
            
            var expectedHeadIndex = (length - 1) & (capacity - 1);
            Assert.That(anatomy.HeadIndex, Is.EqualTo(expectedHeadIndex), "HeadIndex should be calculated correctly.");

            var expectedNextHeadIndex = length & (capacity - 1);
            Assert.That(anatomy.NextHeadIndex, Is.EqualTo(expectedNextHeadIndex), "NextHeadIndex should be calculated correctly.");
            
            var isFull = length == capacity;
            Assert.That(anatomy.IsFull, Is.EqualTo(isFull), "IsFull should be right.");
        });
    }
    
     // --- Test per il metodo PopTail ---

     [Test(Description = "Verifica che PopTail aggiorni correttamente lo stato senza verificare wrap-around.")]
     [TestCase(1)]
     [TestCase(2)]
     [TestCase(3)]
     [TestCase(4)]
     [TestCase(int.MaxValue)]
     public void PopTail_ShouldUpdateStateCorrectly(int ratio)
     {
         // Arrange
         var length = ratio == int.MaxValue ? capacity : capacity / ratio;

         var anatomy = new Anatomy(capacity, length);
        
         // Act
         anatomy.PopTail();

         // Assert: Verifica che solo TailIndex e le proprietà calcolate cambino.
         Assert.Multiple(() =>
         {
             Assert.That(anatomy.Capacity, Is.EqualTo(capacity), "Capacity should not change.");
             Assert.That(anatomy.Length, Is.EqualTo(length), "Length should not change on PopTail.");

             const int expectedTailIndex = 1;
             Assert.That(anatomy.TailIndex, Is.EqualTo(expectedTailIndex), "TailIndex should increment.");
             
             var expectedHeadIndex = (expectedTailIndex + length - 1) & (capacity - 1);
             Assert.That(anatomy.HeadIndex, Is.EqualTo(expectedHeadIndex), "HeadIndex should be recalculated.");
             
             var expectedNextHeadIndex = (expectedTailIndex + length) & (capacity - 1);
             Assert.That(anatomy.NextHeadIndex, Is.EqualTo(expectedNextHeadIndex), "NextHeadIndex should be recalculated.");
             
             var isFull = length == capacity;
             Assert.That(anatomy.IsFull, Is.EqualTo(isFull), "IsFull should be right.");
         });
     }

     [Test(Description = "Verifica che PopTail aggiorni correttamente lo stato quando si è a piena capacità.")]
     public void PopTail_WhenAtBufferEnd_ShouldWrapAround()
     {
         // Arrange
         var length = capacity;
         var anatomy = new Anatomy(capacity, length);

         // Act
         anatomy.PopTail();

         // Assert: Verifica il comportamento del wrap-around.
         Assert.Multiple(() =>
         {
             const int expectedTailIndex = 1, expectedNextHeadIndex = 1;
             
             Assert.That(anatomy.Capacity, Is.EqualTo(capacity));
             Assert.That(anatomy.Length, Is.EqualTo(length));
             Assert.That(anatomy.TailIndex, Is.EqualTo(expectedTailIndex), "TailIndex should increase by 1.");
             
             const int expectedHeadIndex = 0;
             Assert.That(anatomy.HeadIndex, Is.EqualTo(expectedHeadIndex));
             
             
             Assert.That(anatomy.NextHeadIndex, Is.EqualTo(expectedNextHeadIndex));
             
             Assert.That(anatomy.IsFull, Is.True, "IsFull should be right.");
         });
     }
     
     [Test(Description = "Verifica che PopTail aggiorni correttamente lo stato quando si è a piena capacità.")]
     public void PopTail_WhenAtBufferEnd_ShouldWrapAround1()
     {
         // Arrange
         var length = capacity;
         var anatomy = new Anatomy(capacity, length);

         // Act
         for(var i = 0; i < capacity; i++) anatomy.PopTail();

         // Assert: Verifica il comportamento del wrap-around.
         Assert.Multiple(() =>
         {
             const int expectedTailIndex = 0, expectedNextHeadIndex = 0;
             
             Assert.That(anatomy.Capacity, Is.EqualTo(capacity));
             Assert.That(anatomy.Length, Is.EqualTo(length));
             Assert.That(anatomy.TailIndex, Is.EqualTo(expectedTailIndex), "TailIndex should increase by 1.");
             
             var expectedHeadIndex = capacity - 1;
             Assert.That(anatomy.HeadIndex, Is.EqualTo(expectedHeadIndex));
             
             Assert.That(anatomy.NextHeadIndex, Is.EqualTo(expectedNextHeadIndex));
             
             Assert.That(anatomy.IsFull, Is.True, "IsFull should be right.");
         });
     }

     // --- Test per il metodo IncrementLength ---

     [TestCase(2)]
     [TestCase(3)]
     [TestCase(4)] 
     public void IncrementLength_WhenNotFull_ShouldUpdateStateCorrectly(int ratio)
     {
         // Arrange
         var length = capacity / ratio;
         var anatomy = new Anatomy(capacity, length);

         // Act
         anatomy.IncrementLength();

         // Assert: Verifica che solo Length e le proprietà calcolate cambino.
         Assert.Multiple(() =>
         {
             Assert.That(anatomy.Capacity, Is.EqualTo(capacity), "Capacity should not change.");
             Assert.That(anatomy.Length, Is.EqualTo(length + 1), "Length should increment.");
             Assert.That(anatomy.TailIndex, Is.EqualTo(0), "TailIndex should not change on IncrementLength.");
             
             var expectedHeadIndex = (0 + length) & (capacity - 1); // length + 1 - 1 = length
             Assert.That(anatomy.HeadIndex, Is.EqualTo(expectedHeadIndex), "HeadIndex should be recalculated.");
             
             var expectedNextHeadIndex = (0 + length + 1) & (capacity - 1);
             Assert.That(anatomy.NextHeadIndex, Is.EqualTo(expectedNextHeadIndex), "NextHeadIndex should be recalculated.");
             
             Assert.That(anatomy.IsFull, Is.False, "IsFull should be false when not at capacity.");
         });
     }

     [Test(Description = "Verifica che IncrementLength non modifichi lo stato quando si è già a piena capacità.")]
     public void IncrementLength_WhenAtCapacity_ShouldNotChangeState()
     {
         // Arrange
         var length = capacity;
         var anatomy = new Anatomy(capacity, length);
         
         // Salviamo lo stato iniziale per confronto
         var initialHeadIndex = anatomy.HeadIndex;
         var initialNextHeadIndex = anatomy.NextHeadIndex;

         // Act
         anatomy.IncrementLength();

         // Assert: Verifica che NESSUNA proprietà sia cambiata.
         Assert.Multiple(() =>
         {
             Assert.That(anatomy.Capacity, Is.EqualTo(capacity), "Capacity should remain the same.");
             Assert.That(anatomy.Length, Is.EqualTo(capacity), "Length should not change when at capacity.");
             Assert.That(anatomy.TailIndex, Is.Zero, "TailIndex should not change.");
             Assert.That(anatomy.HeadIndex, Is.EqualTo(initialHeadIndex), "HeadIndex should not change.");
             Assert.That(anatomy.NextHeadIndex, Is.EqualTo(initialNextHeadIndex), "NextHeadIndex should not change.");
                Assert.That(anatomy.IsFull, Is.True, "IsFull should remain true when at capacity.");
         });
     }
}