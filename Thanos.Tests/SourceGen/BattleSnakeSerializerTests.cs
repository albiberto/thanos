using Thanos.SourceGen;
using static NUnit.Framework.Assert;

namespace Thanos.Tests.SourceGen;

[TestFixture]
public class BattleSnakeSerializerTests
{
    // Testiamo l'integrazione completa usando i file REALI del progetto (MediumRequest.json e SmallRequest.json)
    
    [Test]
    public void Parse_WhenMediumRequest_ThenDeserializesAllFieldsCorrectly()
    {
        var request = BattleSnakeSerializer.Parse(Support.MediumJson);

        // --- 1. GAME META ---
        Multiple(() =>
        {
            // Dati dal MediumRequest.json originale
            That(request.Game.Ruleset.Settings.FoodSpawnChance, Is.EqualTo(15));
            That(request.Game.Ruleset.Settings.HazardDamagePerTurn, Is.EqualTo(14));
        });

        // --- 2. GLOBAL TURN & DIMENSIONS ---
        Multiple(() =>
        {
            That(request.Turn, Is.EqualTo(42));
            That(request.Board.Width, Is.EqualTo(11));
            That(request.Board.Height, Is.EqualTo(11));
        });

        // --- 3. FOOD (4 items) ---
        // {5,5}->60, {0,0}->0, {7,8}->95, {4,9}->103
        Multiple(() =>
        {
            That(request.Board.Food, Has.Length.EqualTo(4));
            That(request.Board.Food[0], Is.EqualTo(60));
            That(request.Board.Food[1], Is.EqualTo(0));
            That(request.Board.Food[2], Is.EqualTo(95));
            That(request.Board.Food[3], Is.EqualTo(103));
        });

        // --- 4. SNAKES (4 snakes) ---
        Multiple(() => { That(request.Board.Snakes, Has.Length.EqualTo(4)); });

        // Hero: "snake-hero"
        var hero = request.Board.Snakes[0];
        Multiple(() =>
        {
            That(hero.Id, Is.EqualTo("snake-hero"));
            That(hero.Health, Is.EqualTo(90));
            // Body: {1,0}, {0,0}, {0,1} -> [1, 0, 11]
            That(hero.Body[0], Is.EqualTo(1));
            That(hero.Body[1], Is.EqualTo(0));
            That(hero.Body[2], Is.EqualTo(11));
        });

        // Enemy 1: "snake-enemy"
        var enemy1 = request.Board.Snakes[1];
        Multiple(() =>
        {
            That(enemy1.Id, Is.EqualTo("snake-enemy"));
            That(enemy1.Health, Is.EqualTo(80));
            // Body: {5,5}, {5,6}, {6,6} -> [60, 71, 72]
            That(enemy1.Body[0], Is.EqualTo(60));
        });

        // --- 5. YOU OBJECT ---
        That(request.You.Id, Is.EqualTo("snake-hero"));
    }

    [Test]
    public void Parse_WhenSmallRequest_ThenDetectsWidth7AndCalculatesCoordinatesCorrectly()
    {
        // Questo verifica che PeekBoardWidth funzioni e switchi il contesto
        var request = BattleSnakeSerializer.Parse(Support.SmallJson);

        Multiple(() =>
        {
            That(request.Board.Width, Is.EqualTo(7), "Should detect Width 7");
            
            // In SmallRequest.json il cibo è a {2,2}.
            // Se width=7 -> 2*7 + 2 = 16.
            // Se width=11 -> 2*11 + 2 = 24.
            That(request.Board.Food[0], Is.EqualTo(16), "Should use Width 7 for coordinate calculation");
        });
    }
}