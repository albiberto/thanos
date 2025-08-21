using Thanos.War.Grid;

namespace Thanos.Tests.Tests.WarGridTests;

[TestFixture]
public class BitboardTests
{
    [Test(Description = "Verifica che Set imposti correttamente un bit e IsSet lo legga.")]
    public void Set_And_IsSet_ShouldWorkForSingleBit()
    {
        // Arrange
        // Creiamo un buffer di memoria per il bitboard. 4 ulong = 4 * 64 = 256 bit totali.
        var buffer = new ulong[4]; 
        var bitboard = new Bitboard(buffer);
        const ushort positionToSet = 75; // Un bit a caso nel secondo ulong.

        // Act
        bitboard.Set(positionToSet);

        // Assert
            Assert.That(bitboard.IsSet(positionToSet), Is.True, "Il bit impostato deve risultare 'true'.");
            Assert.That(bitboard.IsSet(positionToSet + 1), Is.False, "Un bit adiacente non deve essere stato modificato.");
            
            // Verifichiamo direttamente il valore grezzo in memoria
            // position 75 -> index 1 (75/64), bit 11 (75%64)
            // Il valore atteso è 1 spostato a sinistra di 11 posizioni.
            ulong expectedValue = 1UL << 11;
            Assert.That(buffer[1], Is.EqualTo(expectedValue), "Il valore raw dell'ulong deve corrispondere alla maschera del bit.");
    }

    [Test(Description = "Verifica che Clear spenga un bit precedentemente impostato.")]
    public void Clear_ShouldTurnOffSetBit()
    {
        // Arrange
        var buffer = new ulong[4];
        var bitboard = new Bitboard(buffer);
        const ushort position = 128; // Primo bit del terzo ulong
        
        bitboard.Set(position); // Impostiamo il bit
        Assert.That(bitboard.IsSet(position), Is.True, "Pre-condizione: il bit deve essere impostato.");

        // Act
        bitboard.Clear(position);

        // Assert
            Assert.That(bitboard.IsSet(position), Is.False, "Il bit spento deve risultare 'false'.");
            Assert.That(buffer[2], Is.EqualTo(0UL), "Il valore raw dell'ulong deve essere tornato a zero.");
    }

    [Test(Description = "Verifica che l'operazione su un bit non modifichi gli altri bit nello stesso ulong.")]
    public void Set_And_Clear_ShouldNotAffectOtherBitsInSameUlong()
    {
        // Arrange
        var buffer = new ulong[4];
        var bitboard = new Bitboard(buffer);
        const ushort positionToClear = 5;
        const ushort positionToKeep = 10;
        
        // Impostiamo due bit nello stesso ulong (il primo)
        bitboard.Set(positionToClear);
        bitboard.Set(positionToKeep);

        // Act
        bitboard.Clear(positionToClear);

        // Assert
            Assert.That(bitboard.IsSet(positionToClear), Is.False, "Il bit 5 deve essere stato spento.");
            Assert.That(bitboard.IsSet(positionToKeep), Is.True, "Il bit 10 deve essere rimasto acceso.");
    }
    
    // Usiamo TestCase per testare in modo efficiente diversi casi limite critici
    [TestCase(0, TestName = "Edge Case: Primo bit dell'intero buffer (0)")]
    [TestCase(63, TestName = "Edge Case: Ultimo bit del primo ulong (63)")]
    [TestCase(64, TestName = "Edge Case: Primo bit del secondo ulong (64)")]
    [TestCase(255, TestName = "Edge Case: Ultimo bit di un buffer da 256 bit")]
    public void Operations_ShouldWorkOnEdgeCases(int p)
    {
        var position = (ushort)p;
        // Arrange
        var buffer = new ulong[4]; // 256 bits
        var bitboard = new Bitboard(buffer);

        // Assert 1: Inizialmente il bit è spento
        Assert.That(bitboard.IsSet(position), Is.False, "Il bit dovrebbe essere inizialmente spento.");
        
        // Act 1: Impostiamo il bit
        bitboard.Set(position);
        
        // Assert 2: Ora il bit è acceso
        Assert.That(bitboard.IsSet(position), Is.True, "Il bit dovrebbe essere acceso dopo Set().");
        
        // Act 2: Spegniamo il bit
        bitboard.Clear(position);
        
        // Assert 3: Ora il bit è di nuovo spento
        Assert.That(bitboard.IsSet(position), Is.False, "Il bit dovrebbe essere spento dopo Clear().");
    }
}