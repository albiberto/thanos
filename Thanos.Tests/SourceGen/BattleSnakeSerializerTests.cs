using Thanos.SourceGen;
using static NUnit.Framework.Assert;

namespace Thanos.Tests.SourceGen;

[TestFixture]
public class BattleSnakeSerializerTests
{
    [Test]
    public void Parse_Should_CorrectlyDeserialize_FullGameRequest_WithThreePartSnakes()
    {
        var json = File.ReadAllText("Requests/SampleRequest.json");

        var request = BattleSnakeSerializer.Parse(json);

        // --- GLOBAL CHECKS ---
        using (EnterMultipleScope())
        {
            That(request.Turn, Is.EqualTo(42));
            That(request.Board.Width, Is.EqualTo(11));
            That(request.Board.Snakes, Has.Length.EqualTo(4));
        }

        // --- HERO (Snake 0) ---
        var hero = request.Board.Snakes[0];
        using (EnterMultipleScope())
        {
            That(hero.Id, Is.EqualTo("snake-hero"));
            
            That(hero.Body, Has.Length.EqualTo(3));

            That(hero.Body[0], Is.EqualTo(1));
            That(hero.Body[1], Is.EqualTo(0)); 
            That(hero.Body[2], Is.EqualTo(11));
        }

        // --- ENEMY 1 (Center) ---
        var enemy1 = request.Board.Snakes[1];
        using (EnterMultipleScope())
        {
            That(enemy1.Id, Is.EqualTo("snake-enemy"));
            
            That(enemy1.Body, Has.Length.EqualTo(3));
            
            That(enemy1.Body[0], Is.EqualTo(60));
            That(enemy1.Body[1], Is.EqualTo(71));
            That(enemy1.Body[2], Is.EqualTo(72));
        }

        // --- ENEMY 2 (Top Right) ---
        var enemy2 = request.Board.Snakes[2];
        using (EnterMultipleScope())
        {
            That(enemy2.Id, Is.EqualTo("snake-enemy-2"));
            
            That(enemy2.Body, Has.Length.EqualTo(3));

            That(enemy2.Body[0], Is.EqualTo(10));
            That(enemy2.Body[1], Is.EqualTo(9));
            That(enemy2.Body[2], Is.EqualTo(8));
        }

        // --- ENEMY 3 (Bottom Right) ---
        var enemy3 = request.Board.Snakes[3];
        using (EnterMultipleScope())
        {
            That(enemy3.Id, Is.EqualTo("snake-enemy-3"));

            That(enemy3.Body, Has.Length.EqualTo(3));
            
            That(enemy3.Body[0], Is.EqualTo(120));
            That(enemy3.Body[1], Is.EqualTo(109));
            That(enemy3.Body[2], Is.EqualTo(108));
        }

        using (EnterMultipleScope())
        {
            That(request.You.Id, Is.EqualTo("snake-hero"));
        }
    }
}