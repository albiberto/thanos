using NUnit.Framework;
using Thanos.CollisionMatrix;
using Thanos.Tests.Support;

namespace Thanos.Tests;

[TestFixture]
public class GetValidMovesTests
{
    [Test]
    public void TestGetValidMoves()
    {
        // Arrange
        var corners = Faker.GetAllScenarios("corners");
        var borders = Faker.GetAllScenarios("borders");
        var cornerHazards = Faker.GetAllScenarios("corners-hazard");
        var enemies = Faker.GetAllScenarios("enemies");
        
        // var scenarios = corners.Concat(borders).Concat(cornerHazards).Concat(enemies).ToList();
        
        // Debug.PrintHeader();

        // scenarios = [scenarios.Last()];
        var scenarios = enemies.Where(a => a.Id > 122).ToList();
        
        foreach (var scenario in scenarios) 
        {
            var request = scenario.MoveRequest;
            
            var board = request.Board;
            var mySnake = request.You;

            var width = board.width;
            var height = board.height;
            var hazards = board.hazards;
            var hazardCount = hazards.Length;
            var snakes = board.snakes;
            var snakeCount = snakes.Length;
            var myId = mySnake.id;
            var myBody = mySnake.body;
            var myBodyLength = myBody.Length;
            var myHead = mySnake.head;
            var myHeadX = myHead.x;
            var myHeadY = myHead.y;
            var eat = mySnake.health == 100;
            
            Debug.PrintMap(width, height, myBody, hazards, snakes, scenario.Expected, scenario.Id, scenario.Name, scenario.FileName, scenario.Id);
        }
        
        return;

        foreach (var scenario in scenarios)
        {
            var request = scenario.MoveRequest;
            
            var scenarioId = scenario.Id;
            var scenarioName = scenario.Name;
            var expected = scenario.Expected;
            
            var board = request.Board;
            var mySnake = request.You;

            var width = board.width;
            var height = board.height;
            var hazards = board.hazards;
            var hazardCount = hazards.Length;
            var snakes = board.snakes;
            var snakeCount = snakes.Length;
            var myId = mySnake.id;
            var myBody = mySnake.body;
            var myBodyLength = myBody.Length;
            var myHead = mySnake.head;
            var myHeadX = myHead.x;
            var myHeadY = myHead.y;
            var eat = mySnake.health == 100;
            
            // Debug    
            Debug.PrintMap(width, height, myBody, hazards, snakes, scenario.Expected, scenario.Id, scenario.Name, scenario.FileName, scenario.Id);
        
            // Act
            var result = GetValidMovesLightSpeedB.GetValidMovesLightSpeed(width, height, myId, myBody, myBodyLength, myHeadX, myHeadY, hazards, hazardCount, snakes, snakeCount, eat);
        
            if(result == scenario.Expected)
            {
                Console.WriteLine();
                Console.WriteLine("🎉🎉🎉🎉🎉🎉🎉🎉🎉🎉🎉🎉🎉🎉🎉🎉🎉🎉🎉🎉🎉🎉🎉");
                Console.WriteLine($"✅ Scenario '{scenarioName}' PASSATO!");
                Console.WriteLine($"Atteso: {expected}");
                Console.WriteLine($"Ottenuto: {result}");
                Console.WriteLine("🎉🎉🎉🎉🎉🎉🎉🎉🎉🎉🎉🎉🎉🎉🎉🎉🎉🎉🎉🎉🎉🎉🎉");
            }
            else
            {
                Console.WriteLine();
                Console.WriteLine("💀💀💀💀💀💀💀💀💀💀💀💀💀💀💀💀💀💀💀💀💀💀💀");
                Console.WriteLine($"❌ Scenario '{scenarioName}' FALLITO!");
                Console.WriteLine($"Atteso: {expected}");
                Console.WriteLine($"Ottenuto: {result}");
                Console.WriteLine("💀💀💀💀💀💀💀💀💀💀💀💀💀💀💀💀💀💀💀💀💀💀💀");
            }
        
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.EqualTo(expected), 
                    $"🆔 ID: {scenario.Id}\n" +
                    $"📝 Scenario: '{scenarioName}'\n" +
                    $"🔎 Atteso: {expected}\n" +
                    $"🛑 Ottenuto: {result}\n" +
                    $"❌ Test fallito!\n"); 
            });
        }
    }
}