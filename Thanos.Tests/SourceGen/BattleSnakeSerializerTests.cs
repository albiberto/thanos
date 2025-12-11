using Thanos.SourceGen;
using static NUnit.Framework.Assert;

namespace Thanos.Tests.SourceGen;

[TestFixture]
public class BattleSnakeSerializerTests
{
    [Test]
    public void Parse_Should_CorrectlyDeserialize_FullGameRequest_WithUpdatedFoodAndHazards()
    {
        var request = BattleSnakeSerializer.Parse(Support.SampleJson);

        // --- 1. GAME META & RULESET CHECKS ---
        Multiple(() =>
        {
            That(request.Game.map, Is.EqualTo("standard"), $"Game.Map should be 'standard' but was '{request.Game.map}'.");
            That(request.Game.Source, Is.EqualTo("league"), $"Game.Source should be 'league' but was '{request.Game.Source}'.");

            var settings = request.Game.Ruleset.Settings;

            That(settings.FoodSpawnChance, Is.EqualTo(15), $"Ruleset.FoodSpawnChance should be 15 but was {settings.FoodSpawnChance}.");
            That(settings.MinimumFood, Is.EqualTo(1), $"Ruleset.MinimumFood should be 1 but was {settings.MinimumFood}.");
            That(settings.HazardDamagePerTurn, Is.EqualTo(14), $"Ruleset.HazardDamagePerTurn should be 14 but was {settings.HazardDamagePerTurn}.");

            That(settings.Royale, Is.Null, $"Ruleset.Royale should be null but was {settings.Royale}.");
            That(settings.Squad, Is.Null, $"Ruleset.Squad should be null but was {settings.Squad}.");
        });

        // --- 2. GLOBAL TURN & BOARD DIMENSIONS ---
        Multiple(() =>
        {
            That(request.Turn, Is.EqualTo(42), $"Turn number should be 42 but was {request.Turn}.");
            That(request.Board.Width, Is.EqualTo(11), $"Board Width should be 11 but was {request.Board.Width}.");
            That(request.Board.Height, Is.EqualTo(11), $"Board Height should be 11 but was {request.Board.Height}.");
        });

        // --- 3. FOOD CHECKS (4 Items) ---
        Multiple(() =>
        {
            That(request.Board.Food, Has.Length.EqualTo(4), $"Food count should be 4 but was {request.Board.Food.Length}.");

            That(request.Board.Food[0], Is.EqualTo(60), $"Food[0] should be 60 but was {request.Board.Food[0]}.");
            That(request.Board.Food[1], Is.EqualTo(0), $"Food[1] should be 0 but was {request.Board.Food[1]}.");
            That(request.Board.Food[2], Is.EqualTo(95), $"Food[2] should be 95 but was {request.Board.Food[2]}.");
            That(request.Board.Food[3], Is.EqualTo(103), $"Food[3] should be 103 but was {request.Board.Food[3]}.");
        });

        // --- 4. HAZARDS CHECKS (11 Items - Full Right Column) ---
        Multiple(() =>
        {
            That(request.Board.Hazards, Has.Length.EqualTo(11), $"Hazards count should be 11 but was {request.Board.Hazards.Length}.");

            That(request.Board.Hazards[0], Is.EqualTo(10), $"Hazards[0] should be 10 but was {request.Board.Hazards[0]}.");
            That(request.Board.Hazards[1], Is.EqualTo(21), $"Hazards[1] should be 21 but was {request.Board.Hazards[1]}.");
            That(request.Board.Hazards[2], Is.EqualTo(32), $"Hazards[2] should be 32 but was {request.Board.Hazards[2]}.");
            That(request.Board.Hazards[3], Is.EqualTo(43), $"Hazards[3] should be 43 but was {request.Board.Hazards[3]}.");
            That(request.Board.Hazards[4], Is.EqualTo(54), $"Hazards[4] should be 54 but was {request.Board.Hazards[4]}.");
            That(request.Board.Hazards[5], Is.EqualTo(65), $"Hazards[5] should be 65 but was {request.Board.Hazards[5]}.");
            That(request.Board.Hazards[6], Is.EqualTo(76), $"Hazards[6] should be 76 but was {request.Board.Hazards[6]}.");
            That(request.Board.Hazards[7], Is.EqualTo(87), $"Hazards[7] should be 87 but was {request.Board.Hazards[7]}.");
            That(request.Board.Hazards[8], Is.EqualTo(98), $"Hazards[8] should be 98 but was {request.Board.Hazards[8]}.");
            That(request.Board.Hazards[9], Is.EqualTo(109), $"Hazards[9] should be 109 but was {request.Board.Hazards[9]}.");
            That(request.Board.Hazards[10], Is.EqualTo(120), $"Hazards[10] should be 120 but was {request.Board.Hazards[10]}.");
        });

        // --- 5. SNAKES CHECKS (4 Snakes) ---
        Multiple(() => { That(request.Board.Snakes, Has.Length.EqualTo(4), $"Snakes count should be 4 but was {request.Board.Snakes.Length}."); });

        // Snake 0: Hero
        var hero = request.Board.Snakes[0];
        Multiple(() =>
        {
            That(hero.Id, Is.EqualTo("snake-hero"), $"Hero.Id should be 'snake-hero' but was '{hero.Id}'.");
            That(hero.Health, Is.EqualTo(90), $"Hero.Health should be 90 but was {hero.Health}.");

            That(hero.Body, Has.Length.EqualTo(3), $"Hero.Body.Length should be 3 but was {hero.Body.Length}.");
            That(hero.Body[0], Is.EqualTo(1), $"Hero.Body[0] should be 1 but was {hero.Body[0]}.");
            That(hero.Body[1], Is.EqualTo(0), $"Hero.Body[1] should be 0 but was {hero.Body[1]}.");
            That(hero.Body[2], Is.EqualTo(11), $"Hero.Body[2] should be 11 but was {hero.Body[2]}.");
        });

        // Snake 1: Enemy 1
        var enemy1 = request.Board.Snakes[1];
        Multiple(() =>
        {
            That(enemy1.Id, Is.EqualTo("snake-enemy"), $"Enemy1.Id should be 'snake-enemy' but was '{enemy1.Id}'.");
            That(enemy1.Health, Is.EqualTo(80), $"Enemy1.Health should be 80 but was {enemy1.Health}.");

            That(enemy1.Body, Has.Length.EqualTo(3), $"Enemy1.Body.Length should be 3 but was {enemy1.Body.Length}.");
            That(enemy1.Body[0], Is.EqualTo(60), $"Enemy1.Body[0] should be 60 but was {enemy1.Body[0]}.");
            That(enemy1.Body[1], Is.EqualTo(71), $"Enemy1.Body[1] should be 71 but was {enemy1.Body[1]}.");
            That(enemy1.Body[2], Is.EqualTo(72), $"Enemy1.Body[2] should be 72 but was {enemy1.Body[2]}.");
        });

        // Snake 2: Enemy 2 (Top Right)
        var enemy2 = request.Board.Snakes[2];
        Multiple(() =>
        {
            That(enemy2.Id, Is.EqualTo("snake-enemy-2"), $"Enemy2.Id should be 'snake-enemy-2' but was '{enemy2.Id}'.");
            That(enemy2.Health, Is.EqualTo(100), $"Enemy2.Health should be 100 but was {enemy2.Health}.");

            That(enemy2.Body, Has.Length.EqualTo(3), $"Enemy2.Body.Length should be 3 but was {enemy2.Body.Length}.");
            That(enemy2.Body[0], Is.EqualTo(10), $"Enemy2.Body[0] should be 10 but was {enemy2.Body[0]}.");
            That(enemy2.Body[1], Is.EqualTo(9), $"Enemy2.Body[1] should be 9 but was {enemy2.Body[1]}.");
            That(enemy2.Body[2], Is.EqualTo(8), $"Enemy2.Body[2] should be 8 but was {enemy2.Body[2]}.");
        });

        // Snake 3: Enemy 3 (Bottom Right)
        var enemy3 = request.Board.Snakes[3];
        Multiple(() =>
        {
            That(enemy3.Id, Is.EqualTo("snake-enemy-3"), $"Enemy3.Id should be 'snake-enemy-3' but was '{enemy3.Id}'.");
            That(enemy3.Health, Is.EqualTo(50), $"Enemy3.Health should be 50 but was {enemy3.Health}.");

            That(enemy3.Body, Has.Length.EqualTo(3), $"Enemy3.Body.Length should be 3 but was {enemy3.Body.Length}.");
            That(enemy3.Body[0], Is.EqualTo(120), $"Enemy3.Body[0] should be 120 but was {enemy3.Body[0]}.");
            That(enemy3.Body[1], Is.EqualTo(109), $"Enemy3.Body[1] should be 109 but was {enemy3.Body[1]}.");
            That(enemy3.Body[2], Is.EqualTo(108), $"Enemy3.Body[2] should be 108 but was {enemy3.Body[2]}.");
        });

        // --- 6. YOU OBJECT CHECKS ---
        That(request.You.Id, Is.EqualTo(hero.Id), $"You.Id should be '{hero.Id}' but was '{request.You.Id}'.");
    }
}