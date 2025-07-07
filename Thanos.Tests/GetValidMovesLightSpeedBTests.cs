using NUnit.Framework;
using Thanos.CollisionMatrix;
using Thanos.Tests.Support;

[TestFixture]
public class GetValidMovesTests
{
    [Test]
    public void TestGetValidMoves()
    {
        // Arrange
        var scenarios = Faker.GetScenarios();

        foreach (var scenario in scenarios)
        {
            var request = scenario.MoveRequest;
            
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
            Debug.Print(width, height, myHeadX, myHeadY, myBody, hazards, snakes, expected, scenarioName);
        
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