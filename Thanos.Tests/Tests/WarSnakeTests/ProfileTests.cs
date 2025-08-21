using Thanos.War.Snake;

namespace Thanos.Tests.Tests.WarSnakeTests;

/// <summary>
/// Unit tests for the Profile struct.
/// </summary>
[TestFixture]
public class ProfileTests
{
    private const int FullHealth = 100;
    
    [TestCase(1, 100, TestName = "Constructor: Should initialize with full health")]
    [TestCase(2, 50, TestName = "Constructor: Should initialize with partial health")]
    public void Constructor_WhenCalled_InitializesPropertiesCorrectly(int expectedId, int expectedHealth)
    {
        // Arrange & Act
        var profile = new Profile(expectedId, expectedHealth);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(profile.Id, Is.EqualTo(expectedId));
            Assert.That(profile.Health, Is.EqualTo(expectedHealth));
        });
    }

    [TestCase(100, false, TestName = "Dead: Should be false for positive health (100)")]
    [TestCase(1, false, TestName = "Dead: Should be false for minimal positive health (1)")]
    [TestCase(0, true, TestName = "Dead: Should be true for zero health")]
    [TestCase(-10, true, TestName = "Dead: Should be true for negative health")]
    public void Dead_ReturnsCorrectStatus_BasedOnHealth(int initialHealth, bool expectedIsDead)
    {
        // Arrange
        var profile = new Profile(1, initialHealth);

        // Act
        var isDead = profile.Dead;

        // Assert
        Assert.That(isDead, Is.EqualTo(expectedIsDead));
    }
    
    [TestCase(75, TestName = "FullCure: Should restore health to 100 from a partial value")]
    [TestCase(0, TestName = "FullCure: Should restore health to 100 from zero")]
    [TestCase(-10, TestName = "FullCure: Should restore health to 100 from a negative value")]
    public void FullCure_WhenCalled_SetsHealthTo100(int initialHealth)
    {
        // Arrange
        var profile = new Profile(1, initialHealth);

        // Act
        profile.FullCure();

        // Assert
        Assert.That(profile.Health, Is.EqualTo(FullHealth));
    }
    
    [TestCase(100, 30, 70, TestName = "Damage: Should reduce health normally")]
    [TestCase(25, 25, 0, TestName = "Damage: Should reduce health to exactly zero")]
    [TestCase(10, 20, -10, TestName = "Damage: Should be able to reduce health below zero")]
    [TestCase(100, 0, 100, TestName = "Damage: Applying zero damage should have no effect")]
    public void Damage_WhenCalled_SubtractsAmountFromHealth(int initialHealth, int damageAmount, int expectedHealth)
    {
        // Arrange
        var profile = new Profile(1, initialHealth);

        // Act
        profile.Damage(damageAmount);

        // Assert
        Assert.That(profile.Health, Is.EqualTo(expectedHealth));
    }
    
    [Test]
    public void Kill_WhenCalled_SetsHealthToZeroAndMarksAsDead()
    {
        // Arrange
        var profile = new Profile(1, FullHealth);

        // Act
        profile.Kill();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(profile.Health, Is.EqualTo(0));
            Assert.That(profile.Dead, Is.True);
        });
    }
}