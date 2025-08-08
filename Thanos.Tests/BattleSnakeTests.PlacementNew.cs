namespace Thanos.Tests;

public partial class BattleSnakeTests
{
    [Test]
    public unsafe void CreateFromState_InitializesStateFromTestCaseCorrectly()
    {
        // ARRANGE and ACT are handled by [TestFixtureSource] and [SetUp].

        // ASSERT: Verify that the snake state matches the test case.
        var expectedHealth = @case.Health;
        var expectedLength = @case.Body.Length;
        var expectedHead = @case.Body[0];
        var expectedTailIndex = expectedLength > 0 ? expectedLength - 1 : 0; // Gestisce il caso di un serpente di lunghezza 0

        Assert.Multiple(() =>
        {
            // Confronta i valori attesi con quelli reali letti da _sut
            Assert.That(_sut->Health, Is.EqualTo(expectedHealth), "Health mismatch");
            Assert.That(_sut->Length, Is.EqualTo(expectedLength), "Length mismatch");
            Assert.That(_sut->Head, Is.EqualTo(expectedHead), "Head mismatch");
            Assert.That(_sut->TailIndex, Is.EqualTo(expectedTailIndex), "TailIndex mismatch");

            // Verifica il contenuto del corpo
            var actualBody = new Span<ushort>(_sut->Body, expectedLength);
            for (var i = 0; i < expectedLength; i++)
            {
                Assert.That(actualBody[i], Is.EqualTo(@case.Body[i]), $"Body content mismatch at index {i}");
            }
        });
    }
}