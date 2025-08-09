using Thanos.Tests.Support;

namespace Thanos.Tests.Parsers;

[TestFixture]
public class LowLevelParserIntegrationTests
{
    private static readonly TestsProvider _testsProvider = new("game", "game_test_cases");

    private static IEnumerable<string> GetTestCaseNames() => _testsProvider.Names;

    [TestCaseSource(nameof(GetTestCaseNames))]
    public void Parse_WithProvidedTestCase_ShouldMatchJsonSerializerResult(string test)
    {
        var @case = _testsProvider[test];
        
        var expectedGame = @case.Game;
        var actualGame = LowLevelParser.Parse(@case.Raw);
        
        AssertGamesAreEqual(actualGame.Game, expectedGame);
    }

    /// <summary>
    /// Helper per confrontare l'oggetto Game prodotto dal parser manuale (struct)
    /// con quello prodotto da JsonSerializer (class).
    /// </summary>
    private void AssertGamesAreEqual(Game actual, Thanos.Tests.Support.Model.Game expected)
    {
        // NOTA: I nomi delle proprietà nei tuoi modelli di test sono diversi (es. map vs Map).
        // Questo helper gestisce queste differenze.
        
        Assert.Multiple(() =>
        {
            // Confronto proprietà dirette di Game
            Assert.That(actual.Id.ToString().ToLowerInvariant(), Is.EqualTo(expected.Id.ToString().ToLowerInvariant()), "Game ID should match.");
            Assert.That(actual.Map.ToString().ToLowerInvariant(), Is.EqualTo(expected.map.ToLowerInvariant()), "Game Map should match.");
            Assert.That(actual.Source.ToString().ToLowerInvariant(), Is.EqualTo(expected.source.ToLowerInvariant()), "Game Source should match.");
            Assert.That(actual.Timeout, Is.EqualTo(expected.timeout), "Game Timeout should match.");

            // Confronto proprietà nidificate in RulesetSettings
            var actualSettings = actual.Ruleset.Settings;
            var expectedSettings = expected.Ruleset.Settings;
            Assert.That(actualSettings.FoodSpawnChance, Is.EqualTo(expectedSettings.foodSpawnChance), "FoodSpawnChance should match.");
            Assert.That(actualSettings.MinimumFood, Is.EqualTo(expectedSettings.minimumFood), "MinimumFood should match.");
            Assert.That(actualSettings.HazardDamagePerTurn, Is.EqualTo(expectedSettings.hazardDamagePerTurn), "HazardDamagePerTurn should match.");

            // Confronto Royale
            if(actualSettings.Royale.HasValue) Assert.That(actualSettings.Royale.Value.ShrinkEveryNTurns, Is.EqualTo(expectedSettings.Royale.ShrinkEveryNTurns), "ShrinkEveryNTurns should match.");

            // Confronto Squad
            if (actualSettings.Squad.HasValue)
            {
                var actualSquad = actualSettings.Squad;
                var expectedSquad = expectedSettings.Squad;
                
                Assert.That(actualSquad.Value.AllowBodyCollisions, Is.EqualTo(expectedSquad.AllowBodyCollisions), "AllowBodyCollisions should match.");
                Assert.That(actualSquad.Value.SharedElimination, Is.EqualTo(expectedSquad.SharedElimination), "SharedElimination should match.");
                Assert.That(actualSquad.Value.SharedHealth, Is.EqualTo(expectedSquad.SharedHealth), "SharedHealth should match.");
                Assert.That(actualSquad.Value.SharedLength, Is.EqualTo(expectedSquad.SharedLength), "SharedLength should match.");   
            }
        });
    }
}