using Thanos.War;
using static NUnit.Framework.Assert;

namespace Thanos.Tests.Integration.WarSnake;

[TestFixture]
public class WarSnakeLifeTests
{
    // Covers the entire byte range (0-255) to ensure no underflow/overflow edge cases are missed.
    public static IEnumerable<TestCaseData> Scenarios => Enumerable
        .Range(0, 256)
        .Select(hp => new TestCaseData((byte)hp));

    private static (byte Hp, bool IsDead) CalculateExpected(byte hp, byte damage = 0)
    {
        var result = hp - damage;
        var expectedHp = (byte)Math.Max(0, result);
        return (expectedHp, expectedHp == 0);
    }

    [TestCaseSource(nameof(Scenarios))]
    public void SetHP_WhenCalled_ShouldUpdateHealth_And_ResetGrowth(byte hp)
    {
        // Arrange
        var life = new WarSnakeLife();

        // Dirty the growth state to ensure SetHP cleans it up
        life.ScheduleGrowth();

        // Act
        life.SetHp(hp);

        // Assert
        var expected = CalculateExpected(hp);

        That(life.Hp, Is.EqualTo(expected.Hp), $"HP mismatch for input {hp}");
        That(life.IsDead, Is.EqualTo(expected.IsDead), $"IsDead mismatch for input {hp}");
        That(life.IsGrowthPending, Is.False, "SetHP must reset any pending growth flag.");
    }

    [TestCaseSource(nameof(Scenarios))]
    public void Damage_WhenNormalMove_ShouldReduceHP_ByOne(byte hp)
    {
        const byte DamageAmount = 1;

        // Arrange
        var life = new WarSnakeLife();
        life.SetHp(hp);

        // Act
        life.Damage(DamageAmount);

        // Assert
        var expected = CalculateExpected(hp, DamageAmount);

        That(life.Hp, Is.EqualTo(expected.Hp), "HP check failed.");
        That(life.IsDead, Is.EqualTo(expected.IsDead), "Death check failed.");
        That(life.IsGrowthPending, Is.False, "Normal damage must not trigger growth.");
    }

    [TestCaseSource(nameof(Scenarios))]
    public void Damage_WhenHazard_ShouldReduceHP_ByTen(byte hp)
    {
        const byte DamageAmount = 10;

        // Arrange
        var life = new WarSnakeLife();
        life.SetHp(hp);

        // Act
        life.Damage(DamageAmount);

        // Assert
        var expected = CalculateExpected(hp, DamageAmount);

        That(life.Hp, Is.EqualTo(expected.Hp), "HP check failed.");
        That(life.IsDead, Is.EqualTo(expected.IsDead), "Death check failed.");
        That(life.IsGrowthPending, Is.False, "Hazard damage must not trigger growth.");
    }

    [TestCaseSource(nameof(Scenarios))]
    public void Kill_WhenCalled_ShouldZeroHP_Immediately(byte hp)
    {
        // Arrange
        var life = new WarSnakeLife();
        life.SetHp(hp);

        // Act
        life.Kill();

        // Assert
        That(life.Hp, Is.Zero, "HP must be 0 after Kill.");
        That(life.IsDead, Is.True, "IsDead must be true after Kill.");
        That(life.IsGrowthPending, Is.False, "Killing the snake must not trigger growth.");
    }

    [TestCaseSource(nameof(Scenarios))]
    public void FullCure_WhenCalled_ShouldRestoreMaxHealth(byte initialHp)
    {
        const byte ExpectedMaxHealth = 100;

        // Arrange
        var life = new WarSnakeLife();
        life.SetHp(initialHp);

        // Act
        life.FullCure();

        // Assert
        // Note: FullCure technically resurrects a dead snake (0 HP -> 100 HP).
        That(life.Hp, Is.EqualTo(ExpectedMaxHealth), "FullCure must restore exactly 100 HP.");
        That(life.IsDead, Is.False, "Snake must be alive after FullCure.");
        That(life.IsGrowthPending, Is.False, "FullCure (healing only) must not trigger growth by itself.");
    }

    [TestCaseSource(nameof(Scenarios))]
    public void ScheduleGrowth_WhenCalled_ShouldSetPendingFlag(byte initialHp)
    {
        // Arrange
        var life = new WarSnakeLife();
        life.SetHp(initialHp);

        // Act
        life.ScheduleGrowth();

        // Assert
        That(life.IsGrowthPending, Is.True, "ScheduleGrowth did not set the pending flag.");
    }

    [TestCaseSource(nameof(Scenarios))]
    public void ConsumePendingGrowth_WhenFlagIsSet_ShouldReturnTrue_And_ResetFlag(byte initialHp)
    {
        // Arrange
        var life = new WarSnakeLife();
        life.SetHp(initialHp);
        life.ScheduleGrowth();

        // Act
        var result = life.ConsumePendingGrowth();

        // Assert
        That(result, Is.True, "Should return true when growth was pending.");
        That(life.IsGrowthPending, Is.False, "Flag must be consumed (reset) after reading.");
    }

    [TestCaseSource(nameof(Scenarios))]
    public void ConsumePendingGrowth_WhenFlagIsNotSet_ShouldReturnFalse(byte initialHp)
    {
        // Arrange
        var life = new WarSnakeLife();
        life.SetHp(initialHp);

        // Act
        var result = life.ConsumePendingGrowth();

        // Assert
        That(result, Is.False, "Should return false when no growth is pending.");
    }

    [TestCaseSource(nameof(Scenarios))]
    public void ScheduleGrowth_WhenEatingConsecutively_ShouldReArmFlag(byte initialHp)
    {
        // Arrange
        var life = new WarSnakeLife();
        life.SetHp(initialHp);

        // --- TURN 1: Eat Food ---
        life.ScheduleGrowth();
        var growTurn1 = life.ConsumePendingGrowth();

        // --- TURN 2: Eat Food Again (Chain) ---
        life.ScheduleGrowth();
        var growTurn2 = life.ConsumePendingGrowth();

        // Assert
        That(growTurn1, Is.True, "Turn 1: Snake should grow.");
        That(growTurn2, Is.True, "Turn 2: Snake should grow again (Flag re-arming failed).");
        That(life.IsGrowthPending, Is.False, "Flag must be reset after the second consumption.");
    }
    
    [Test]
    public void ScheduleGrowth_WhenExceedingMaxByte_ShouldOverflowToZero()
    {
        // Arrange
        var life = new WarSnakeLife();
        life.SetHp(100);

        const int maxCredits = 255;

        // Act & Assert: Accumulation Phase (0 -> 255)
        // Verify increment integrity up to the physical limit
        for (var i = 1; i <= maxCredits; i++)
        {
            life.ScheduleGrowth();

            That(life.IsGrowthPending, Is.True, $"Growth must be pending after {i} increments.");
            That(life.Credits, Is.EqualTo((byte)i), $"Internal Credits counter mismatch at step {i}.");
        }

        // Act: Overflow Trigger (255 -> 0)
        // We explicitly accept the overflow for performance reasons (Branchless)
        life.ScheduleGrowth(); // The 256th call

        // Assert: Verify Wrap-Around
        That(life.Credits, Is.Zero, 
            "Credits must overflow to 0 after exceeding byte.MaxValue (Performance decision).");
    
        That(life.IsGrowthPending, Is.False, 
            "Growth must NOT be pending after wrapping around to 0.");

        // Verification: Ensure consumption fails on empty/overflowed state
        var result = life.ConsumePendingGrowth();
        That(result, Is.False, "Cannot consume growth after overflow reset.");
    }
}