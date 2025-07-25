﻿using NUnit.Framework;
using Thanos.CollisionMatrix;
using Thanos.Tests.Support;

namespace Thanos.Tests;

[TestFixture]
public class GetValidMovesTests
{
    [Test]
    public void TestGetValidMoves()
    {
        // Arrange - Load all existing scenarios
        var basicsJson = Faker.GetAllScenarios("01-basics");
        var hazardsJson = Faker.GetAllScenarios("02-hazards");
        var h2hJson = Faker.GetAllScenarios("03-head2head");
        var foodJson = Faker.GetAllScenarios("04-food");
        // var combat = Faker.GetAllScenarios("05-combat");
        var complexJson = Faker.GetAllScenarios("10-complex");
        
        // Combine all scenarios for comprehensive testing
        var scenarios =
            Enumerable.Empty<Scenario>()
                .Concat(basicsJson)
                .Concat(hazardsJson)
                .Concat(h2hJson)
                .Concat(foodJson)
                .Concat(complexJson)
                .Concat([]);

        scenarios = basicsJson;
        
        // Visual(scenarios);
        // return;
        Debug.PrintHeader();
        
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
            Debug.PrintMap(width, height, myBody, hazards, snakes, scenario.Expected, scenario.Id, scenario.Name, scenario.FileName, scenario.Id, true, false, true);
        
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

    private static void Visual(IEnumerable<Scenario> scenarios)
    {
        const bool onlyFailed = false;
        const bool onlyBoards = true;
        const bool noDirections = !true;
        
        // For testing specific scenarios, uncomment and modify as needed:
        scenarios = scenarios.Where(a => a.Id is >= 100 and < 200); // Basic movements
        // scenarios = scenarios.Where(a => a.Id is >= 200 and < 300); // Borders
        // scenarios = scenarios.Where(a => a.Id is >= 300 and < 400); // Hazards
        // scenarios = scenarios.Where(a => a.Id is >= 400 and < 500); // Space control
        // scenarios = scenarios.Where(a => a.Id is >= 500 and < 600); // Combat
        // scenarios = scenarios.Where(a => a.Id is >= 1000);          // Complex 
        // scenarios = complex;
        
        // Debug.PrintHeader();
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
            
            Debug.PrintMap(width, height, myBody, hazards, snakes, scenario.Expected, scenario.Id, scenario.Name, scenario.FileName, scenario.Id, onlyFailed, onlyBoards, noDirections);
        }
    }
}