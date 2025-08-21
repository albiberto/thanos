using Thanos.War.Snake;

namespace Thanos.Tests.Tests.WarSnakeTests;

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
    public void Dead_Property_ShouldReturnCorrectStatus(int hp, bool expectedIsDead)
    {
        // Arrange & Act
        var health = new Health(hp);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(health.IsDead, Is.EqualTo(expectedIsDead), "Dead status should match expected value.");
            Assert.That(health.HealthPoints, Is.EqualTo(hp), "HealthPoints should match the initial health value.");
        });
    }
    
    [TestCase(100, TestName = "FullCure: Should restore health to 100")]
    [TestCase(1, TestName = "FullCure: Should restore health to 100 from minimal positive health")]
    [TestCase(0, TestName = "FullCure: Should restore health to 100 from zero health")]
    [TestCase(-10, TestName = "FullCure: Should restore health to 100 from negative health")]
    public void FullCure_WhenCalledOnDeadProfile_ShouldReviveIt(int hp)
    {
        // Arrange
        var health = new Health(hp);

        // Act
        health.FullCure();

        Assert.Multiple(() =>
        {
            // Assert: Verifichiamo che non sia più morto
            Assert.That(health.IsDead, Is.False);
            Assert.That(health.HealthPoints, Is.EqualTo(100), "Health should be restored to 100 after FullCure.");
        });
    }
    
    [TestCase(100, 30, false, TestName = "Damage: Non-fatal damage should not kill")]
    [TestCase(30, 30, true, TestName = "Damage: Fatal damage (exact) should kill")]
    [TestCase(20, 30, true, TestName = "Damage: Fatal damage (overkill) should kill")]
    public void Damage_WhenCalled_ShouldUpdateDeadStatusCorrectly(int initialHealth, int damageAmount, bool expectedIsDead)
    {
        // Arrange
        var health = new Health(initialHealth);
        var expectedHealth = initialHealth - damageAmount;

        // Act
        health.Damage(damageAmount);

        Assert.Multiple(() =>
        {
            // Assert: Controlliamo solo se il danno è stato fatale o meno
            Assert.That(health.IsDead, Is.EqualTo(expectedIsDead));
            Assert.That(health.HealthPoints, Is.EqualTo(expectedHealth), "HealthPoints should be updated correctly after damage.");
        });
    }
    
    [TestCase(100, TestName = "Kill: Should set health to 0")]
    [TestCase(1, TestName = "Kill: Should set health to 0 from minimal positive health")]
    [TestCase(0, TestName = "Kill: Should set health to 0 from zero health")]
    [TestCase(-10, TestName = "Kill: Should set health to 0 from negative health")]
    public void Kill_WhenCalled_ShouldResultInDeadStatus(int hp)
    {
        // Arrange
        var health = new Health(hp);

        // Act
        health.Kill();

        Assert.Multiple(() =>
        {
            // Assert: L'unica cosa che possiamo e dobbiamo verificare è che ora sia morto.
            Assert.That(health.IsDead, Is.True);
            Assert.That(health.HealthPoints, Is.EqualTo(0), "HealthPoints should be set to 0 after Kill.");
        });
    }
}