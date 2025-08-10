using System.Text.Json;
using Thanos.SourceGen;
using Thanos.Tests.Support;
using Thanos.Tests.Support.Model;

namespace Thanos.Tests;

[TestFixture]
public class LowLevelParserIntegrationTests
{
    private static readonly TestsProvider _testsProvider = new("battle-snake_tests");

    private static IEnumerable<string> GetTestCaseNames() => _testsProvider.Names;

    [TestCaseSource(nameof(GetTestCaseNames))]
    public void Parse_WithProvidedTestCase_ShouldMatchStandardSerializerResult(string testCaseName)
    {
        var rawJson = _testsProvider[testCaseName];

        // JsonSerializer (expected) - **Aggiunte le opzioni necessarie**
        var expectedOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var expected = JsonSerializer.Deserialize<TestRequest>(rawJson, expectedOptions);

        // Source Generator (actual)
        var actual = JsonSerializer.Deserialize(rawJson, ThanosSerializerContext.Default.Request);

        if (expected is null) Assert.Fail("Failed to deserialize the 'expected' TestRequest object.");

        AssertRequestsAreEqual(expected!, actual);
    }

    private static void AssertRequestsAreEqual(TestRequest expected, Request actual)
    {
        Assert.Multiple(() =>
        {
            AssertGamesAreEqual(expected.Game, actual.Game);
            
            Assert.That(actual.Turn, Is.EqualTo(expected.Turn), "Turn should match.");

            AssertBoardsAreEqual(expected.Board, actual.Board);
            
            AssertSnakesAreEqual(expected.You, actual.You, "You");
        });
    }

    private static void AssertGamesAreEqual(TestGame expected, Game actual)
    {
        Assert.Multiple(() =>
        {
            Assert.That(expected.Id, Is.EqualTo(actual.Id), "Game.Id should match.");
            Assert.That(expected.Map, Is.EqualTo(actual.Map.ToString()).IgnoreCase, "Game.Map should match.");
            Assert.That(expected.Source, Is.EqualTo(actual.Source.ToString()).IgnoreCase, "Game.Source should match.");
            Assert.That(expected.Timeout, Is.EqualTo(actual.Timeout), "Game.Timeout should match.");

            AssertRulesetsAreEqual(expected.Ruleset, actual.Ruleset);
        });
    }

    private static void AssertRulesetsAreEqual(TestRuleset expected, Ruleset actual)
    {
        Assert.Multiple(() =>
        {
            Assert.That(expected.Name, Has.Length.GreaterThan(1), "Expected Ruleset.Name should have length > 1.");
            Assert.That(expected.Version, Does.Match(@"v\d+\.\d+\.\d+"), "Expected Ruleset.Version should match the expected pattern.");

            AssertRulesetSettingsAreEqual(expected.Settings, actual.Settings);
        });
    }

    private static void AssertRulesetSettingsAreEqual(TestRulesetSettings expected, RulesetSettings actual)
    {
        Assert.Multiple(() =>
        {
            Assert.That(expected.FoodSpawnChance, Is.EqualTo(actual.FoodSpawnChance), "Settings.FoodSpawnChance should match.");
            Assert.That(expected.MinimumFood, Is.EqualTo(actual.MinimumFood), "Settings.MinimumFood should match.");
            Assert.That(expected.HazardDamagePerTurn, Is.EqualTo(actual.HazardDamagePerTurn), "Settings.HazardDamagePerTurn should match.");

            Assert.That(expected.Royale is not null, Is.EqualTo(actual.Royale.HasValue), "Royale null state should match.");
            if (expected.Royale is not null && actual.Royale.HasValue) Assert.That(expected.Royale.ShrinkEveryNTurns, Is.EqualTo(actual.Royale.Value.ShrinkEveryNTurns), "Royale.ShrinkEveryNTurns should match.");

            Assert.That(expected.Squad is not null, Is.EqualTo(actual.Squad.HasValue), "Squad null state should match.");
            if (expected.Squad is not null && actual.Squad.HasValue) AssertSquadAreEqual(expected.Squad, actual.Squad.Value);
        });
    }

    private static void AssertSquadAreEqual(TestSquad expected, Squad actual)
    {
        Assert.Multiple(() =>
        {
            Assert.That(expected.AllowBodyCollisions, Is.EqualTo(actual.AllowBodyCollisions), "Squad.AllowBodyCollisions should match.");
            Assert.That(expected.SharedElimination, Is.EqualTo(actual.SharedElimination), "Squad.SharedElimination should match.");
            Assert.That(expected.SharedHealth, Is.EqualTo(actual.SharedHealth), "Squad.SharedHealth should match.");
            Assert.That(expected.SharedLength, Is.EqualTo(actual.SharedLength), "Squad.SharedLength should match.");
        });
    }

    private static void AssertBoardsAreEqual(TestBoard expected, Board actual)
    {
        Assert.Multiple(() =>
        {
            Assert.That(actual.Height, Is.EqualTo(expected.Height), "Board.Height should match.");
            Assert.That(actual.Width, Is.EqualTo(expected.Width), "Board.Width should match.");

            AssertCollectionsAreEqual(expected.Food, actual.Food, "Board.Food", AssertCoordinatesAreEqual);
            AssertCollectionsAreEqual(expected.Hazards, actual.Hazards, "Board.Hazards", AssertCoordinatesAreEqual);
            AssertCollectionsAreEqual(expected.Snakes, actual.Snakes, "Board.Snakes", AssertSnakesAreEqual);
        });
    }

    private static void AssertSnakesAreEqual(TestSnake expected, Snake actual, string context)
    {
        Assert.Multiple(() =>
        {
            Assert.That(expected.Id, Is.EqualTo(actual.Id), $"{context}.Id should match.");
            // Assert.That(expected.Name, Is.EqualTo(actual.Name), $"{context}.Name should match.");
            Assert.That(expected.Health, Is.EqualTo(actual.Health), $"{context}.Health should match.");
            // Assert.That(expected.Latency, Is.EqualTo(actual.Latency), $"{context}.Latency should match.");
            Assert.That(expected.Length, Is.EqualTo(actual.Length), $"{context}.Length should match.");
            // Assert.That(expected.Shout, Is.EqualTo(actual.Shout), $"{context}.Shout should match.");

            AssertCoordinatesAreEqual(expected.Head, actual.Head, $"{context}.Head");
            // AssertCustomizationsAreEqual(expected.Customizations, actual.Customizations $"{context}.Customizations");
            AssertCollectionsAreEqual(expected.Body, actual.Body, $"{context}.Body", AssertCoordinatesAreEqual);
        });
    }

    private static void AssertCoordinatesAreEqual(TestCoordinate expected, Coordinate actual, string context)
    {
        Assert.Multiple(() =>
        {
            Assert.That(actual.X, Is.EqualTo(expected.X), $"{context}.X should match.");
            Assert.That(actual.Y, Is.EqualTo(expected.Y), $"{context}.Y should match.");
        });
    }

    private static void AssertCustomizationsAreEqual(TestCustomizations expected, Customizations actual, string context)
    {
        Assert.Multiple(() =>
        {
            Assert.That(expected.Color, Is.EqualTo(actual.Color), $"{context}.Color should match.");
            Assert.That(expected.Head, Is.EqualTo(actual.Head), $"{context}.Head should match.");
            Assert.That(expected.Tail, Is.EqualTo(actual.Tail), $"{context}.Tail should match.");
        });
    }

    private static void AssertCollectionsAreEqual<TExpected, TActual>(ICollection<TExpected> expectedCollection, ICollection<TActual> actualCollection, string collectionName, Action<TExpected, TActual, string> elementComparer)
    {
        Assert.That(expectedCollection, Has.Count.EqualTo(actualCollection.Count), $"{collectionName} count should match.");
        
        var zip = expectedCollection.Zip(actualCollection, (expected, actual) => (Expected: expected, Actual: actual))
            .Select((zip, i) => (zip.Expected, zip.Actual, Index: i))
            .ToList();

        foreach (var pair in zip) elementComparer(pair.Expected, pair.Actual, $"{collectionName}[{pair.Index}]");
    }
}