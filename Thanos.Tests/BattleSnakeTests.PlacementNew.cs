namespace Thanos.Tests;

/// <summary>
///     Contains tests for validating the initial state of the BattleSnake.
/// </summary>
public partial class SnakeTests
{
    /// <summary>
    ///     Verifies that CreateFromState correctly initializes the snake based on the TestCase.
    /// </summary>
    [Test]
    public unsafe void CreateFromState_InitializesStateFromTestCaseCorrectly()
    {
        // Arrange and Act are performed by [TestFixtureSource] and [SetUp]

        // Expected values from the test case
        var expectedHealth = @case.Health;
        var expectedLength = @case.Body.Length;
        var expectedHead = @case.Body[0];
        var expectedTailIndex = expectedLength > 0 ? expectedLength - 1 : 0;

        // Assert: validate snake state
        Assert.Multiple(() =>
        {
            // Check core fields: health, length, head, tail index
            Assert.That(_sut->Health, Is.EqualTo(expectedHealth), "Health mismatch");
            Assert.That(_sut->Length, Is.EqualTo(expectedLength), "Length mismatch");
            Assert.That(_sut->Head, Is.EqualTo(expectedHead), "Head mismatch");
            Assert.That(_sut->TailIndex, Is.EqualTo(expectedTailIndex), "TailIndex mismatch");

            // Validate the full body content
            var actualBody = new Span<ushort>(_sut->Body, expectedLength);
            for (var i = 0; i < expectedLength; i++) Assert.That(actualBody[i], Is.EqualTo(@case.Body[i]), $"Body content mismatch at index {i}");
        });
    }
}