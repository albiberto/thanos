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
        _sut->Move(newHeadPosition, false);

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
        _sut->Move(newHeadPosition, true);

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

    [Test]
    public unsafe void Move_WhenEatingUntilFull_ReachesMaxCapacityAndStateIsCorrect()
    {
        // ARRANGE
        // Lo stato iniziale è fornito da `@case` e creato nel [SetUp].

        // Calcoliamo quante volte il serpente deve mangiare per riempire la sua capacità.
        var movesToFill = @case.Capacity - @case.Body.Length;

        // Se il serpente è già pieno, non c'è nulla da fare. Il test è valido.
        if (movesToFill <= 0)
        {
            Assert.That(_sut->Length, Is.EqualTo(@case.Capacity));
            Assert.Pass("Snake started at full capacity, test is valid.");
            return;
        }

        // Prendiamo la posizione iniziale della testa per calcolare le mosse successive.
        var initialHead = @case.Body[0];

        // ACT
        // Simuliamo il serpente che mangia fino a riempirsi.
        for (var i = 1; i <= movesToFill; i++)
        {
            // La nuova posizione della testa è sequenziale per avere un risultato prevedibile.
            var nextHeadPosition = (ushort)(initialHead + i);
            _sut->Move(nextHeadPosition, true);
        }

        // ASSERT
        // Verifichiamo che lo stato finale sia quello di un serpente saturo e corretto.
        Assert.Multiple(() =>
        {
            // 1. La lunghezza DEVE essere uguale alla capacità.
            Assert.That(_sut->Length, Is.EqualTo(@case.Capacity), "Snake should be at full capacity.");

            // 2. La salute deve essere 100, perché l'ultima mossa era con cibo.
            Assert.That(_sut->Health, Is.EqualTo(100), "Health should be reset to 100 after eating.");

            // 3. La testa deve trovarsi nell'ultima posizione calcolata.
            var expectedHead = (ushort)(initialHead + movesToFill);
            Assert.That(_sut->Head, Is.EqualTo(expectedHead), "Head is not at the final expected position.");

            // 4. L'indice della coda NON deve essere cambiato.
            // Dato che il serpente ha solo mangiato per crescere, non ha mai mosso la sua coda originale.
            // L'indice della coda, quindi, deve rimanere quello impostato inizialmente da CreateFromState.
            var expectedTailIndex = @case.Body.Length > 0 ? @case.Body.Length - 1 : 0;
            Assert.That(_sut->TailIndex, Is.EqualTo(expectedTailIndex), "TailIndex should not have moved during growth phase.");
        });
    }
}