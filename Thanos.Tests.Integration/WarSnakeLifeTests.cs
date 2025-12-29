using Thanos.War;
using static NUnit.Framework.Assert;

namespace Thanos.Tests.Integration;

[TestFixture]
public class WarSnakeLifeTests
{
    public static IEnumerable<TestCaseData> Scenarios => Enumerable
        .Range(0, 256)
        .Select(hp => new TestCaseData((byte)hp));

    private static (byte Hp, bool IsDead) CalculateExpected(byte hp, byte damage = 0)
    {
        var result = hp - damage;
        var expectedHp = (byte)Math.Max(0, result);
        return (expectedHp, expectedHp == 0);
    }

    // TEST 1: Inizializzazione
    [TestCaseSource(nameof(Scenarios))]
    public void SetHP_ShouldUpdateHealth_AndDetermineDeadStatus(byte hp)
    {
        // Arrange
        var life = new WarSnakeLife();

        // Act
        life.SetHP(hp);

        // Assert
        var expected = CalculateExpected(hp);

        Multiple(() =>
        {
            That(life.HP, Is.EqualTo(expected.Hp), $"[SetHP] Input: {hp} -> HP non allineati.");
            That(life.IsDead, Is.EqualTo(expected.IsDead), $"[SetHP] Input: {hp} -> Stato IsDead errato.");
        });
    }

    // TEST 2: Danno Normale (1)
    [TestCaseSource(nameof(Scenarios))]
    public void Damage_ShouldReduceHP_ByOne_OnNormalMove(byte hp)
    {
        const byte DamageAmount = 1;

        // Arrange
        var life = new WarSnakeLife();
        life.SetHP(hp);

        // Act
        life.Damage(DamageAmount);

        // Assert
        var expected = CalculateExpected(hp, DamageAmount);

        Multiple(() =>
        {
            That(life.HP, Is.EqualTo(expected.Hp), $"[Move Damage] StartHP: {hp} - Damage: {DamageAmount}. Expected HP: {expected.Hp}, but was: {life.HP}.");
            That(life.IsDead, Is.EqualTo(expected.IsDead), $"[Move Damage] StartHP: {hp} - Damage: {DamageAmount}. Expected Dead: {expected.IsDead}, but was: {life.IsDead}.");
        });
    }

    // TEST 3: Danno Hazard (10)
    [TestCaseSource(nameof(Scenarios))]
    public void Damage_ShouldReduceHP_ByTen_OnHazard(byte hp)
    {
        const byte DamageAmount = 10;

        // Arrange
        var life = new WarSnakeLife();
        life.SetHP(hp);

        // Act
        life.Damage(DamageAmount);

        // Assert
        var expected = CalculateExpected(hp, DamageAmount);

        Multiple(() =>
        {
            That(life.HP, Is.EqualTo(expected.Hp), $"[Hazard Damage] StartHP: {hp} - Damage: {DamageAmount}. Expected HP: {expected.Hp}, but was: {life.HP}.");
            That(life.IsDead, Is.EqualTo(expected.IsDead), $"[Hazard Damage] StartHP: {hp} - Damage: {DamageAmount}. Expected Dead: {expected.IsDead}, but was: {life.IsDead}.");
        });
    }

    // TEST 4: Kill Istantanea
    [TestCaseSource(nameof(Scenarios))]
    public void Kill_ShouldSetHPToZero_Immediately(byte hp)
    {
        // Arrange
        var life = new WarSnakeLife();
        life.SetHP(hp);

        // Act
        life.Kill();

        // Assert
        Multiple(() =>
        {
            That(life.HP, Is.Zero, $"[Kill] StartHP: {hp}. Expected HP: 0, but was: {life.HP}.");
            That(life.IsDead, Is.True, $"[Kill] StartHP: {hp}. Expected Dead: True, but was: {life.IsDead}.");
        });
    }

    // TEST 5: Cura Completa (FullCure)
    // Nota: FullCure forza HP a 100 anche se HP era 0. 
    // Questo è il comportamento corretto della struct (Resurrezione tecnica).
    [TestCaseSource(nameof(Scenarios))]
    public void FullCure_ShouldRestoreMaxHealth(byte initialHp)
    {
        const byte ExpectedMaxHealth = 100;

        // Arrange
        var life = new WarSnakeLife();
        life.SetHP(initialHp);

        // Act
        life.FullCure();

        // Assert
        Multiple(() =>
        {
            That(life.HP, Is.EqualTo(ExpectedMaxHealth), $"[FullCure] StartHP: {initialHp}. Expected HP: {ExpectedMaxHealth}, but was: {life.HP}.");
            That(life.IsDead, Is.False, $"[FullCure] StartHP: {initialHp}. Expected Dead: False, but was: {life.IsDead}.");
        });
    }
}