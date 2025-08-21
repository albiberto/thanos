using NUnit.Framework;
using Thanos.War.Snake;

namespace Thanos.Tests.War.Snake;

/// <summary>
/// Unit tests for the fully encapsulated Health struct.
/// Tests are based on the observable 'Dead' state, not the internal health value.
/// </summary>
[TestFixture]
public class HealthTests
{
    // Il test del costruttore è stato rimosso perché non ci sono più proprietà
    // pubbliche da verificare. La sua correttezza viene testata indirettamente
    // attraverso gli altri test che usano la proprietà 'Dead'.

    [TestCase(100, false, TestName = "Dead: Should be false for positive health")]
    [TestCase(1, false, TestName = "Dead: Should be false for minimal positive health")]
    [TestCase(0, true, TestName = "Dead: Should be true for zero health")]
    [TestCase(-10, true, TestName = "Dead: Should be true for negative health")]
    public void Dead_Property_ShouldReturnCorrectStatus(int initialHealth, bool expectedIsDead)
    {
        // Arrange & Act
        var health = new Health(initialHealth);

        // Assert
        Assert.That(health.Dead, Is.EqualTo(expectedIsDead));
    }
    
    [Test]
    public void FullCure_WhenCalledOnDeadProfile_ShouldReviveIt()
    {
        // Arrange: Partiamo da uno stato "morto"
        var health = new Health(0);
        Assert.That(health.Dead, Is.True, "Precondition: Health should be Dead initially.");

        // Act
        health.FullCure();

        // Assert: Verifichiamo che non sia più morto
        Assert.That(health.Dead, Is.False);
    }
    
    [TestCase(100, 30, false, TestName = "Damage: Non-fatal damage should not kill")]
    [TestCase(30, 30, true, TestName = "Damage: Fatal damage (exact) should kill")]
    [TestCase(20, 30, true, TestName = "Damage: Fatal damage (overkill) should kill")]
    public void Damage_WhenCalled_ShouldUpdateDeadStatusCorrectly(int initialHealth, int damageAmount, bool expectedIsDead)
    {
        // Arrange
        var health = new Health(initialHealth);

        // Act
        health.Damage(damageAmount);

        // Assert: Controlliamo solo se il danno è stato fatale o meno
        Assert.That(health.Dead, Is.EqualTo(expectedIsDead));
    }
    
    [Test]
    public void Kill_WhenCalled_ShouldResultInDeadStatus()
    {
        // Arrange
        var health = new Health(100);
        Assert.That(health.Dead, Is.False, "Precondition: Health should be alive initially.");

        // Act
        health.Kill();

        // Assert: L'unica cosa che possiamo e dobbiamo verificare è che ora sia morto.
        Assert.That(health.Dead, Is.True);
    }
}