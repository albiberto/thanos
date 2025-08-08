namespace Thanos.Tests;

public partial class BattleSnakeTests
{
    [Test]
    public unsafe void Kill_KillSnake_SetHealthToZeroAndDoesntTouchOtherProperties()
    {
        // ARRANGE
        // Salviamo lo stato iniziale completo prima di qualsiasi azione
        var initialLength = _sut->Length;
        var initialHead = _sut->Head;
        var initialTailIndex = _sut->TailIndex;

        _sut->Kill(); // Uccidiamo il serpente

        // ASSERT
        Assert.Multiple(() =>
        {
            Assert.That(_sut->Dead, Is.True, "Snake should be dead.");
            Assert.That(_sut->Length, Is.EqualTo(initialLength), "Length should not change on a dead snake.");
            Assert.That(_sut->Head, Is.EqualTo(initialHead), "Head should not change on a dead snake.");
            Assert.That(_sut->TailIndex, Is.EqualTo(initialTailIndex), "TailIndex should not change on a dead snake.");
        });
    }
}