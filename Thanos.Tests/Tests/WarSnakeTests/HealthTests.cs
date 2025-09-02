using Thanos.War.Snake;

namespace Thanos.Tests.Tests.WarSnakeTests;

/// <summary>
///     Contains all unit tests for the Health struct.
///     Tests verify the state transitions (e.g., becoming dead) and the behavior
///     of methods that mutate the health value (Damage, FullCure, Kill).
/// </summary>
[TestFixture]
public class HealthTests
{
    // =================================================================
    // State Verification Tests
    // =================================================================

    [TestCase(100, false, TestName = "IsDead is false for positive health")]
    [TestCase(1, false, TestName = "IsDead is false for minimal positive health")]
    [TestCase(0, true, TestName = "IsDead is true for zero health")]
    [TestCase(-10, true, TestName = "IsDead is true for negative health")]
    [Test(Description = "Verifies the IsDead property accurately reflects the health points (HP).")]
    public void IsDead_Property_ShouldReturnCorrectStatus(int hp, bool expectedIsDead)
    {
        // Arrange & Act
        var health = new Health(hp);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(health.IsDead, Is.EqualTo(expectedIsDead), "The IsDead status should be correct based on the initial HP.");
            Assert.That(health.HealthPoints, Is.EqualTo(hp), "HealthPoints should correctly report the initial value.");
        });
    }

    // =================================================================
    // Method Behavior Tests
    // =================================================================

    [TestCase(100, TestName = "From full health")]
    [TestCase(1, TestName = "From minimal positive health")]
    [TestCase(0, TestName = "From zero health (revival)")]
    [TestCase(-10, TestName = "From negative health (revival)")]
    [Test(Description = "Ensures FullCure resets health to 100 and makes the entity alive, regardless of the initial state.")]
    public void FullCure_ShouldAlwaysRestoreHealthTo100(int initialHp)
    {
        // Arrange
        var health = new Health(initialHp);

        // Act
        health.FullCure();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(health.IsDead, Is.False, "Entity should be alive after a FullCure.");
            Assert.That(health.HealthPoints, Is.EqualTo(100), "HealthPoints should be exactly 100 after FullCure.");
        });
    }

    [TestCase(100, 30, false, TestName = "Non-fatal damage should not kill")]
    [TestCase(30, 30, true, TestName = "Fatal damage (exact) should kill")]
    [TestCase(20, 30, true, TestName = "Fatal damage (overkill) should kill")]
    [Test(Description = "Verifies that applying damage correctly updates the health points and the IsDead status.")]
    public void Damage_ShouldUpdateStateCorrectly(int initialHp, int damageAmount, bool expectedIsDead)
    {
        // Arrange
        var health = new Health(initialHp);
        var expectedHealth = initialHp - damageAmount;

        // Act
        health.Damage(damageAmount);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(health.IsDead, Is.EqualTo(expectedIsDead), "The IsDead status should reflect whether the damage was fatal.");
            Assert.That(health.HealthPoints, Is.EqualTo(expectedHealth), "HealthPoints should be correctly reduced by the damage amount.");
        });
    }

    [TestCase(100, TestName = "From positive health")]
    [TestCase(0, TestName = "From zero health")]
    [TestCase(-10, TestName = "From negative health")]
    [Test(Description = "Ensures the Kill method sets health to 0 and results in a dead state.")]
    public void Kill_ShouldSetHealthToZeroAndResultInDeath(int initialHp)
    {
        // Arrange
        var health = new Health(initialHp);

        // Act
        health.Kill();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(health.IsDead, Is.True, "Entity must be dead after Kill() is called.");
            Assert.That(health.HealthPoints, Is.EqualTo(0), "HealthPoints should be exactly 0 after Kill().");
        });
    }
}