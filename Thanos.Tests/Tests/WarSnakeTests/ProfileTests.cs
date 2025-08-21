using Thanos.War.Snake;

namespace Thanos.Tests.Tests.WarSnakeTests;

[TestFixture]
public class ProfileTests
{
    private const int fullHealth = 100;
    
    [TestCase(1, 100)]
    [TestCase(2, 75)]
    [TestCase(3, 50)]
    [TestCase(4, 25)]  
    public void Constructor_WhenCalled_InitializesPropertiesCorrectly(int expectedId, int expectedHealth)
    {
        var profile = new Profile(expectedId, expectedHealth);

        Assert.Multiple(() =>
        {
            Assert.That(profile.Id, Is.EqualTo(expectedId));
            Assert.That(profile.Health, Is.EqualTo(expectedHealth));
        });
    }

    [TestCase(100, false)] // Salute piena, non è morto
    [TestCase(75, false)] // Salute 3/4, non è morto
    [TestCase(50, false)] // Salute 2/4, non è morto
    [TestCase(25, false)] // Salute 1/4, non è morto
    [TestCase(1, false)]   // Salute minima, non è morto
    [TestCase(0, true)]    // Salute a zero, è morto
    [TestCase(-1, true)]  // Salute negativa, è morto
    public void Dead_ReturnsCorrectStatus_BasedOnHealth(int initialHealth, bool expectedIsDead)
    {
        var profile = new Profile(1, initialHealth);

        var isDead = profile.Dead;

        Assert.That(isDead, Is.EqualTo(expectedIsDead));
    }
    
    [TestCase(100)]
    [TestCase(75)]
    [TestCase(50)] 
    [TestCase(25)] 
    [TestCase(1)] 
    [TestCase(0)]
    [TestCase(-1)]
    public void FullCure_WhenCalled_SetsHealthTo100(int health)
    {
        var profile = new Profile(1, health);

        profile.FullCure();

        Assert.That(profile.Health, Is.EqualTo(fullHealth));
    }
    
    [TestCase(100, 75)]
    [TestCase(75, 50)]
    [TestCase(50, 25)] 
    [TestCase(25, 1)] 
    [TestCase(1, 1)] 
    [TestCase(0, 1)] 
    [TestCase(-1, 1)] 
    public void Damage_WhenCalled_SubtractsAmountFromHealth(int health, int damage)
    {
        var profile = new Profile(1, fullHealth);
        var expectedHealth = fullHealth - damage;

        profile.Damage(damage);

        Assert.That(profile.Health, Is.EqualTo(expectedHealth));
    }
    
    [Test]
    public void Kill_WhenCalled_SetsHealthToZero()
    {
        var profile = new Profile(1, 100);

        profile.Kill();

        Assert.Multiple(() =>
        {
            Assert.That(profile.Health, Is.EqualTo(0));
            Assert.That(profile.Dead, Is.True);
        });
    }
}