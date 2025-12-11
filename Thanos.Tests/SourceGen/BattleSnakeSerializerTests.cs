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

        // --- GLOBAL CHECKS ---
        Multiple(() =>
        {
            That(request.Turn, Is.EqualTo(42));
            
            That(request.Board.Width, Is.EqualTo(11));
            That(request.Board.Height, Is.EqualTo(11));
            
            That(request.Board.Snakes, Has.Length.EqualTo(4));
        });

        // --- FOOD CHECKS ---
        Multiple(() =>
        {
            That(request.Board.Food, Has.Length.EqualTo(4));
            
            That(request.Board.Food[0], Is.EqualTo(60));  // (5,5)
            That(request.Board.Food[1], Is.EqualTo(0));   // (0,0)
            That(request.Board.Food[2], Is.EqualTo(95));  // (7,8) -> 8*11 + 7
            That(request.Board.Food[3], Is.EqualTo(103)); // (4,9) -> 9*11 + 4
        });

        // --- HAZARDS CHECKS ---
        Multiple(() =>
        {
            That(request.Board.Hazards, Has.Length.EqualTo(11));

            That(request.Board.Hazards[0], Is.EqualTo(10));  // y=0
            That(request.Board.Hazards[1], Is.EqualTo(21));  // y=1
            That(request.Board.Hazards[2], Is.EqualTo(32));  // y=2
            That(request.Board.Hazards[3], Is.EqualTo(43));  // y=3
            That(request.Board.Hazards[4], Is.EqualTo(54));  // y=4
            That(request.Board.Hazards[5], Is.EqualTo(65));  // y=5
            That(request.Board.Hazards[6], Is.EqualTo(76));  // y=6
            That(request.Board.Hazards[7], Is.EqualTo(87));  // y=7
            That(request.Board.Hazards[8], Is.EqualTo(98));  // y=8
            That(request.Board.Hazards[9], Is.EqualTo(109)); // y=9
            That(request.Board.Hazards[10], Is.EqualTo(120)); // y=10
        });

        // --- HERO (Snake 0) ---
        var hero = request.Board.Snakes[0];
        Multiple(() =>
        {
            That(hero.Id, Is.EqualTo(Support.Me));

            That(hero.Body, Has.Length.EqualTo(3));
            That(hero.Body[0], Is.EqualTo(1));
            That(hero.Body[1], Is.EqualTo(0));
            That(hero.Body[2], Is.EqualTo(11));
        });

        // --- ENEMY 1 (Center) ---
        var enemy1 = request.Board.Snakes[1];
        Multiple(() =>
        {
            That(enemy1.Id, Is.EqualTo(Support.Enemy1));
        
            That(enemy1.Body, Has.Length.EqualTo(3));
            That(enemy1.Body[0], Is.EqualTo(60));
            That(enemy1.Body[1], Is.EqualTo(71));
            That(enemy1.Body[2], Is.EqualTo(72));
        });

        // --- ENEMY 2 (Top Right) ---
        var enemy2 = request.Board.Snakes[2];
        Multiple(() =>
        {
            That(enemy2.Id, Is.EqualTo(Support.Enemy2));
            
            That(enemy2.Body, Has.Length.EqualTo(3));
            That(enemy2.Body[0], Is.EqualTo(10));
            That(enemy2.Body[1], Is.EqualTo(9));
            That(enemy2.Body[2], Is.EqualTo(8));
        });

        // --- ENEMY 3 (Bottom Right) ---
        var enemy3 = request.Board.Snakes[3];
        Multiple(() =>
        {
            That(enemy3.Id, Is.EqualTo(Support.Enemy3));
            
            That(enemy3.Body, Has.Length.EqualTo(3));
            That(enemy3.Body[0], Is.EqualTo(120));
            That(enemy3.Body[1], Is.EqualTo(109));
            That(enemy3.Body[2], Is.EqualTo(108));
        });

        That(request.You.Id, Is.EqualTo("snake-hero"));
    }
}