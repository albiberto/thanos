using Thanos.War;
using static NUnit.Framework.Assert;

namespace Thanos.Tests.Integration;

[TestFixture]
public class WarSnakeLifeTests
{
    // TEST 1: Inizializzazione e SetHP
    [TestCase((byte)100, false)]
    [TestCase((byte)1, false)]
    [TestCase((byte)0, true)]
    public void SetHP_ShouldUpdateHealth_AndDetermineDeadStatus(byte hp, bool expectedDead)
    {
        var life = new WarSnakeLife();
        
        life.SetHP(hp);

        Multiple(() =>
        {
            That(life.HP, Is.EqualTo(hp), "HP non corrisponde al valore impostato.");
            That(life.IsDead, Is.EqualTo(expectedDead), "Stato IsDead errato.");
        });
    }

    // TEST 2: Danno Normale
    [Test]
    public void Damage_ShouldReduceHP_WithoutKilling()
    {
        var life = new WarSnakeLife();
        life.SetHP(100);

        life.Damage(10);

        Multiple(() =>
        {
            That(life.HP, Is.EqualTo(90));
            That(life.IsDead, Is.False);
        });
    }

    // TEST 3: Danno Letale (Overkill)
    // Verifica che non ci sia underflow del byte (es. 10 - 20 -> 246) ma che vada a 0
    [Test]
    public void Damage_WhenOverkill_ShouldClampToZero_AndKill()
    {
        var life = new WarSnakeLife();
        life.SetHP(10);

        life.Damage(20); // Danno superiore alla vita attuale

        Multiple(() =>
        {
            That(life.HP, Is.Zero, "HP dovrebbe essere 0 (clamped).");
            That(life.IsDead, Is.True, "Il serpente dovrebbe essere morto.");
        });
    }

    // TEST 4: Logica di Crescita (Growth)
    // Verifica il ciclo: Schedule -> Pending -> Consume -> Reset
    [Test]
    public void GrowthCycle_ShouldWorkCorrectly()
    {
        var life = new WarSnakeLife();
        life.SetHP(100);

        // 1. Inizialmente nessuna crescita
        That(life.IsGrowthPending, Is.False, "Non dovrebbe esserci crescita pendente all'inizio.");

        // 2. Schedulazione
        life.ScheduleGrowth();
        That(life.IsGrowthPending, Is.True, "IsGrowthPending dovrebbe essere true dopo ScheduleGrowth.");

        // 3. Consumo (Primo tentativo)
        var consumed = life.ConsumePendingGrowth();
        Multiple(() =>
        {
            That(consumed, Is.True, "ConsumePendingGrowth dovrebbe tornare true la prima volta.");
            That(life.IsGrowthPending, Is.False, "IsGrowthPending dovrebbe tornare false dopo il consumo.");
        });

        // 4. Consumo (Secondo tentativo - Già consumato)
        var consumedAgain = life.ConsumePendingGrowth();
        That(consumedAgain, Is.False, "ConsumePendingGrowth dovrebbe tornare false se non c'è crescita pendente.");
    }

    // TEST 5: Kill Istantanea
    [Test]
    public void Kill_ShouldSetHPToZero_Immediately()
    {
        var life = new WarSnakeLife();
        life.SetHP(100);

        life.Kill();

        Multiple(() =>
        {
            That(life.HP, Is.Zero);
            That(life.IsDead, Is.True);
        });
    }

    // TEST 6: Cura Completa (FullCure)
    // Usata quando si mangia il cibo
    [Test]
    public void FullCure_ShouldRestoreMaxHealth()
    {
        var life = new WarSnakeLife();
        life.SetHP(10); // Quasi morto

        life.FullCure();

        Multiple(() =>
        {
            That(life.HP, Is.EqualTo(100), "FullCure dovrebbe riportare HP a 100.");
            That(life.IsDead, Is.False);
        });
    }
}