namespace Thanos.Tests;

public partial class BattleSnakeTests
{
    [Test]
    public unsafe void Move_OnDeadSnake_HasNoEffect()
    {
        // Arrange
        const ushort startingHead = 42;
        
        _sut->Initialize(startingHead, Capacity);
        _sut->Kill(); // L'azione di "uccidere" fa parte della preparazione

        // Calcola lo stato finale atteso (deve essere identico a quello dopo Kill)
        const bool expectedIsDead = true;
        const int expectedLength = 1;
        var expectedHealth = 0;
        var expectedHead = startingHead;
        var expectedTailIndex = 0;
    
        // Act
        _sut->Move(999, false, 10); // Questa mossa non deve avere alcun effetto

        // Assert
        AssertFinalState(expectedIsDead, expectedLength, expectedHealth, expectedHead, expectedTailIndex);
        // Un'asserzione specifica per il corpo di un serpente appena inizializzato e morto
        Assert.That(_sut->Body[0], Is.EqualTo(startingHead));
        for(var i = 1; i < Capacity; i++) Assert.That(_sut->Body[i], Is.EqualTo(0));
    }
    
    [Test]
    public unsafe void Move_WhenTakingDamage_DiesWhenHealthReachesZero()
    {
        // Arrange
        const ushort startingHead = 50;
        const int damagePerMove = 10;
        const int initialHealth = 100;
        _sut->Initialize(startingHead, Capacity);

        // Calcola lo stato finale atteso
        var movesToDie = initialHealth / damagePerMove;
        var successfulMoves = movesToDie - 1; // Lo stato si congela prima della mossa fatale

        var expectedIsDead = true;
        var expectedLength = 1; // Non ha mai mangiato
        var expectedHealth = 0; // O <= 0
        var expectedHead = (ushort)(startingHead + successfulMoves);
        var expectedTailIndex = successfulMoves; // La coda si muove ad ogni mossa riuscita
    
        // Act
        for (var i = 1; i <= movesToDie; i++) _sut->Move((ushort)(startingHead + i), false, damagePerMove);

        // Assert
        AssertFinalState(expectedIsDead, expectedLength, expectedHealth, expectedHead, expectedTailIndex);
        AssertBody(_sut->Body, startingHead, successfulMoves);
    }
    
    [Test]
    public unsafe void Move_WithoutEating_TakesDamageAndTailMoves()
    {
        // Arrange
        const ushort startingHead = 10;
        const int eatingMoves = 1;
        const int nonEatingMoves = 9; // Eseguiamo 9 mosse senza mangiare
        _sut->Initialize(startingHead, Capacity);

        // Calcola lo stato finale atteso
        var totalMoves = eatingMoves + nonEatingMoves;
        var expectedIsDead = false;
        var expectedLength = 1 + eatingMoves; // Cresce solo di 1
        var expectedHealth = 100 - nonEatingMoves; // Danno solo per le mosse senza mangiare
        var expectedHead = (ushort)(startingHead + totalMoves);
        var movesThatAdvanceTail = totalMoves - eatingMoves; // La coda avanza solo quando non cresce
        var expectedTailIndex = movesThatAdvanceTail;

        // Act
        _sut->Move(startingHead + 1, true); // Mangia una volta
        for (var i = 1; i <= nonEatingMoves; i++) _sut->Move((ushort)(startingHead + 1 + i), false);
    
        // Assert
        AssertFinalState(expectedIsDead, expectedLength, expectedHealth, expectedHead, expectedTailIndex);
        AssertBody(_sut->Body, startingHead, totalMoves);
    }

    [Test]
    [TestCase(Capacity)]
    [TestCase(Capacity + Capacity / 2)]
    [TestCase(Capacity * 2)]
    [TestCase(Capacity * 2 + Capacity / 3)]
    [TestCase(Capacity * 3)]
    [TestCase(Capacity * 3 + Capacity / 4)]
    [TestCase(Capacity * 4)]
    [TestCase(Capacity * 4 + Capacity / 5)]
    public unsafe void Move_WhenEatingContinuously_MaintainsCorrectState(int totalMoves)
    {
        // Arrange
        const ushort startingHead = 1;
        _sut->Initialize(startingHead, Capacity);

        // Calcola lo stato finale atteso
        var expectedIsDead = false;
        var expectedHealth = 100;
        var expectedLength = Math.Min(Capacity, 1 + totalMoves);
        var expectedHead = (ushort)(startingHead + totalMoves);
        var movesThatAdvanceTail = totalMoves >= Capacity ? totalMoves - (Capacity - 1) : 0;
        var expectedTailIndex = movesThatAdvanceTail & (Capacity - 1);

        // Act
        for (var i = 1; i <= totalMoves; i++) _sut->Move((ushort)(startingHead + i), true);
    
        // Assert
        AssertFinalState(expectedIsDead, expectedLength, expectedHealth, expectedHead, expectedTailIndex);
        AssertSaturatedBody(_sut->Body, expectedLength, expectedHead, _sut->TailIndex);
    }
    
    private unsafe void AssertFinalState(
        bool expectedDead, 
        int expectedLength, 
        int expectedHealth, 
        ushort expectedHead, 
        int expectedTailIndex)
    {
        Assert.Multiple(() =>
        {
            // Stato generale
            Assert.That(_sut->Dead, Is.EqualTo(expectedDead), expectedDead ? "Snake should be dead." : "Snake should be alive.");
            Assert.That(_sut->Length, Is.EqualTo(expectedLength), $"Length should be {expectedLength}.");
            Assert.That(_sut->Health, expectedDead ? Is.LessThanOrEqualTo(0) : Is.EqualTo(expectedHealth), $"Health should be {expectedHealth}.");
        
            // Posizione
            Assert.That(_sut->Head, Is.EqualTo(expectedHead), $"Head should be at {expectedHead}.");
            Assert.That(_sut->TailIndex, Is.EqualTo(expectedTailIndex), $"TailIndex should be at {expectedTailIndex}.");
        });
    }

    private static unsafe void AssertBody(ushort* body, int startingHead, int totalMoves)
    {
        Assert.That(body[0], Is.EqualTo(startingHead), $"Mismatch at index {0}: expected {startingHead}, found {body[0]}");
        for (var i = 1; i <= totalMoves; i++) Assert.That(body[i], Is.EqualTo(startingHead + i - 1), $"Mismatch at index {i}: expected {startingHead + i}, found {body[i]}");
    }
    
    private static unsafe void AssertSaturatedBody(ushort* body, int capacity, int head, int tailIndex)
    {
        // 1. Calculate the expected starting value at tail position, and the capacity mask
        var expectedValue = (ushort)(head - (capacity - 1)) - 1;
        var capacityMask = capacity - 1;

        // 2. Verify all elements in the circular buffer
        Assert.Multiple(() =>
        {
            for (var i = 0; i < capacity; i++)
            {
                // 3. Calculate the buffer index to check, starting from the tail
                var bufferIndex = (tailIndex + i) & capacityMask;
    
                // 4. Assert that the value matches the expected sequence
                Assert.That(body[bufferIndex], Is.EqualTo(expectedValue), $"Mismatch at index {bufferIndex}: expected {expectedValue}, found {body[bufferIndex]}");
                
                expectedValue++;
            }
        });
    }
    
    private unsafe void Print(ushort* body)
    {
        for(var i = 0; i < Capacity; i++) Console.WriteLine($"Body[{i}] = {body[i]}");
    }
}