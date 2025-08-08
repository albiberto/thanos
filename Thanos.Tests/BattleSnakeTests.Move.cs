namespace Thanos.Tests;

public partial class BattleSnakeTests
{
    [Test]
    public unsafe void Move_WhenNotEating_LengthIsConstantAndTailMoves()
    {
        // ARRANGE
        // Lo stato iniziale è definito da `@case` e creato nel SetUp.
        var initialLength = _sut->Length;
        var initialTailIndex = _sut->TailIndex;
        const ushort newHeadPosition = 999; // Una posizione qualsiasi

        // ACT
        _sut->Move(newHeadPosition, false, 0);

        // ASSERT
        Assert.Multiple(() =>
        {
            Assert.That(_sut->Length, Is.EqualTo(initialLength), "Length should not change when not eating.");
            Assert.That(_sut->Head, Is.EqualTo(newHeadPosition), "Head should update to the new position.");

            // La coda avanza (con wrap-around del buffer circolare)
            var expectedTailIndex = (initialTailIndex + 1) % @case.Capacity;
            Assert.That(_sut->TailIndex, Is.EqualTo(expectedTailIndex), "TailIndex should advance by one.");
        });
    }

    [Test]
    public unsafe void Move_WhenEating_LengthIncrementsAndTailIsConstant()
    {
        // Non eseguiamo questo test se il serpente è già alla massima capacità
        if (@case.Body.Length >= @case.Capacity)
        {
            Assert.Ignore("Cannot test eating when snake is already at full capacity.");
            return;
        }

        // ARRANGE
        var initialLength = _sut->Length;
        var initialTailIndex = _sut->TailIndex;
        const ushort newHeadPosition = 888;

        // ACT
        _sut->Move(newHeadPosition, true, 0);

        // ASSERT
        Assert.Multiple(() =>
        {
            Assert.That(_sut->Length, Is.EqualTo(initialLength + 1), "Length should increment by 1 when eating.");
            Assert.That(_sut->Head, Is.EqualTo(newHeadPosition));
            Assert.That(_sut->TailIndex, Is.EqualTo(initialTailIndex), "TailIndex should NOT change when eating.");
            Assert.That(_sut->Health, Is.EqualTo(100), "Health should reset to 100 when eating.");
        });
    }

    [Test]
    public unsafe void Move_ReducesHealth_WhenTakingDamage()
    {
        // ARRANGE
        var initialHealth = _sut->Health;
        const int damage = 15;

        // ACT
        _sut->Move(777, false, damage);

        // ASSERT
        // The health should decrease by the damage amount plus 1 (for the move itself)
        Assert.That(_sut->Health, Is.EqualTo(initialHealth - damage - 1));
    }
}