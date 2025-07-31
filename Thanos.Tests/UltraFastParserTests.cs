using Thanos.Parsers;
using Thanos.Tests.Support;

namespace Thanos.Tests;

/// <summary>
/// Test suite per UltraFastParser usando NUnit
/// </summary>
[TestFixture]
public unsafe class UltraFastParserTests
{
    private TestsProvider _testsProvider;

    [OneTimeSetUp]
    public void OneTimeSetUp() => _testsProvider = new TestsProvider();
    
    [Test]
    public void Parse_BasicGameState_ShouldExtractCorrectValues()
    {
        foreach (var name in _testsProvider.Names)
        {
            var test = _testsProvider[name];
            
            var json = test.Json;
            var expected = test.State;
            
            var actual = Initialize(expected.Board.Width, expected.Board.Height);
            UltraFastParser.Parse(json, actual);
            
            Assert.Multiple(() =>
            {
                Assert.That(actual->Turn, Is.EqualTo(expected.Turn), $"Turn mismatch for test: {name}");
                Assert.That(actual->Width, Is.EqualTo(expected.Board.Width), $"Width mismatch for test: {name}");
                Assert.That(actual->Height, Is.EqualTo(expected.Board.Height), $"Height mismatch for test: {name}");
                Assert.That(actual->TotalCells, Is.EqualTo(expected.Board.Width * expected.Board.Height), $"TotalCells mismatch for test: {name}");
                Assert.That(actual->SnakeCount, Is.EqualTo(expected.Board.Snakes.Count), $"SnakeCount mismatch for test: {name}");
                Assert.That(actual->FoodCount, Is.EqualTo(expected.Board.Food.Count), $"FoodCount mismatch for test: {name}");
                Assert.That(actual->HazardCount, Is.EqualTo(expected.Board.Hazards.Count), $"FoodCount mismatch for test: {name}");

                foreach (var expectedSnake in expected.Board.Snakes)
                {
                    var snakes = Enumerable.Range(0, actual->SnakeCount).Select(i => actual->Snakes[i]);
                    var actualSnake = snakes.FirstOrDefault(s => s.Id == expectedSnake.Id);
                    
                    Assert.That(actualSnake.Length, Is.EqualTo(expectedSnake.Body.Count), $"Snake length mismatch per test: {name}, snake: {index}");
                }
            });
        }
    }
    
    private static GameState* Initialize(byte width = 11, byte height = 11)
    {
        GameManager.Initialize(width, height);
        return GameManager.State;
    }
}