using Thanos.CollisionMatrix;
using Thanos.Domain;
using Thanos.Model;
using Thanos.Tests;

[TestClass]
public class GetValidMovesTests
{
    // Costanti per le direzioni (assumendo questi valori)
    private const int ALL_DIRECTIONS = MonteCarlo.UP | MonteCarlo.DOWN | MonteCarlo.LEFT | MonteCarlo.RIGHT;

    #region Test Griglia 11x11

    [TestMethod]
    [DataRow(11u, 11u, 5u, 5u, new uint[] { }, new uint[] { }, new uint[] { }, ALL_DIRECTIONS,  "11x11 - Centro libero")]
    [DataRow(11u, 11u, 0u, 0u, new uint[] { }, new uint[] { }, new uint[] { }, MonteCarlo.DOWN | MonteCarlo.RIGHT,  "11x11 - Angolo TOP LEFT")]
    [DataRow(11u, 11u, 10u, 0u, new uint[] { }, new uint[] { }, new uint[] { }, MonteCarlo.DOWN | MonteCarlo.LEFT,  "11x11 - Angolo TOP RIGHT")]
    [DataRow(11u, 11u, 0u, 10u, new uint[] { }, new uint[] { }, new uint[] { }, MonteCarlo.UP | MonteCarlo.RIGHT,  "11x11 - Angolo BOTTOM LEFT")]
    [DataRow(11u, 11u, 10u, 10u, new uint[] { }, new uint[] { }, new uint[] { }, MonteCarlo.UP | MonteCarlo.LEFT,  "11x11 - Angolo BOTTOM RIGHT")]
    [DataRow(11u, 11u, 5u, 0u, new uint[] { }, new uint[] { }, new uint[] { }, MonteCarlo.DOWN | MonteCarlo.LEFT | MonteCarlo.RIGHT,  "11x11 - Bordo superiore")]
    [DataRow(11u, 11u, 5u, 10u, new uint[] { }, new uint[] { }, new uint[] { }, MonteCarlo.UP | MonteCarlo.LEFT | MonteCarlo.RIGHT,  "11x11 - Bordo inferiore")]
    [DataRow(11u, 11u, 0u, 5u, new uint[] { }, new uint[] { }, new uint[] { }, MonteCarlo.UP | MonteCarlo.DOWN | MonteCarlo.RIGHT,  "11x11 - Bordo sinistro")]
    [DataRow(11u, 11u, 10u, 5u, new uint[] { }, new uint[] { }, new uint[] { }, MonteCarlo.UP | MonteCarlo.DOWN | MonteCarlo.LEFT,  "11x11 - Bordo destro")]
    public void Test_11x11_PositionBoundaries(uint width, uint height, uint headX, uint headY, uint[] hazardCoords, uint[] myBodyCoords, uint[] otherSnakeCoords, int expected, string name) => TestValidMoves(width, height, headX, headY, hazardCoords, myBodyCoords, otherSnakeCoords, expected, name);

    [TestMethod]
    [DataRow(11u, 11u, 5u, 5u, new uint[] { }, new[] { 5u, 5u, 4u, 5u }, new uint[] { }, MonteCarlo.UP | MonteCarlo.DOWN | MonteCarlo.RIGHT,  "11x11 - Corpo a sinistra")]
    [DataRow(11u, 11u, 5u, 5u, new uint[] { }, new[] { 5u, 5u, 6u, 5u }, new uint[] { }, MonteCarlo.UP | MonteCarlo.DOWN | MonteCarlo.LEFT,  "11x11 - Corpo a destra")]
    [DataRow(11u, 11u, 5u, 5u, new uint[] { }, new[] { 5u, 5u, 5u, 4u }, new uint[] { }, MonteCarlo.DOWN | MonteCarlo.LEFT | MonteCarlo.RIGHT,  "11x11 - Corpo sopra")]
    [DataRow(11u, 11u, 5u, 5u, new uint[] { }, new[] { 5u, 5u, 5u, 6u }, new uint[] { }, MonteCarlo.UP | MonteCarlo.LEFT | MonteCarlo.RIGHT,  "11x11 - Corpo sotto")]
    [DataRow(11u, 11u, 5u, 5u, new uint[] { }, new[] { 5u, 5u, 4u, 5u, 6u, 5u }, new uint[] { }, MonteCarlo.UP | MonteCarlo.DOWN,  "11x11 - Corpo a sinistra e destra")]
    [DataRow(11u, 11u, 5u, 5u, new uint[] { }, new[] { 5u, 5u, 5u, 4u, 5u, 6u }, new uint[] { }, MonteCarlo.LEFT | MonteCarlo.RIGHT,  "11x11 - Corpo sopra e sotto")]
    public void Test_11x11_MyBodyCollisions(uint width, uint height, uint headX, uint headY, uint[] hazardCoords, uint[] myBodyCoords, uint[] otherSnakeCoords, int expected, string name)
    {
        TestValidMoves(width, height, headX, headY, hazardCoords, myBodyCoords, otherSnakeCoords, expected, name);
    }
    
    [TestMethod]
    [DataRow(11u, 11u, 5u, 5u, new uint[] { }, new uint[] { }, new[] { 4u, 5u }, MonteCarlo.UP | MonteCarlo.DOWN | MonteCarlo.RIGHT,  "11x11 - Nemico a sinistra")]
    [DataRow(11u, 11u, 5u, 5u, new uint[] { }, new uint[] { }, new[] { 6u, 5u }, MonteCarlo.UP | MonteCarlo.DOWN | MonteCarlo.LEFT,  "11x11 - Nemico a destra")]
    [DataRow(11u, 11u, 5u, 5u, new uint[] { }, new uint[] { }, new[] { 5u, 4u }, MonteCarlo.DOWN | MonteCarlo.LEFT | MonteCarlo.RIGHT,  "11x11 - Nemico sopra")]
    [DataRow(11u, 11u, 5u, 5u, new uint[] { }, new uint[] { }, new[] { 5u, 6u }, MonteCarlo.UP | MonteCarlo.LEFT | MonteCarlo.RIGHT,  "11x11 - Nemico sotto")]
    [DataRow(11u, 11u, 5u, 5u, new uint[] { }, new uint[] { }, new[] { 4u, 5u, 6u, 5u }, MonteCarlo.UP | MonteCarlo.DOWN,  "11x11 - Nemici a sinistra e destra")]
    public void Test_11x11_EnemyCollisions(uint width, uint height, uint headX, uint headY, uint[] hazardCoords, uint[] myBodyCoords, uint[] otherSnakeCoords, int expected, string name)
    {
        TestValidMoves(width, height, headX, headY, hazardCoords, myBodyCoords, otherSnakeCoords, expected, name);
    }
    
    
    [TestMethod]
    [DataRow(11u, 11u, 5u, 5u, new[] { 4u, 5u }, new uint[] { }, new uint[] { }, MonteCarlo.UP | MonteCarlo.DOWN | MonteCarlo.RIGHT,  "11x11 - Hazard a sinistra")]
    [DataRow(11u, 11u, 5u, 5u, new[] { 6u, 5u }, new uint[] { }, new uint[] { }, MonteCarlo.UP | MonteCarlo.DOWN | MonteCarlo.LEFT,  "11x11 - Hazard a destra")]
    [DataRow(11u, 11u, 5u, 5u, new[] { 5u, 4u }, new uint[] { }, new uint[] { }, MonteCarlo.DOWN | MonteCarlo.LEFT | MonteCarlo.RIGHT,  "11x11 - Hazard sopra")]
    [DataRow(11u, 11u, 5u, 5u, new[] { 5u, 6u }, new uint[] { }, new uint[] { }, MonteCarlo.UP | MonteCarlo.LEFT | MonteCarlo.RIGHT,  "11x11 - Hazard sotto")]
    [DataRow(11u, 11u, 5u, 5u, new[] { 4u, 5u, 6u, 5u, 5u, 4u, 5u, 6u }, new uint[] { }, new uint[] { }, 0,  "11x11 - Hazard tutte direzioni")]
    public void Test_11x11_HazardCollisions(uint width, uint height, uint headX, uint headY, uint[] hazardCoords, uint[] myBodyCoords, uint[] otherSnakeCoords, int expected, string name)
    {
        TestValidMoves(width, height, headX, headY, hazardCoords, myBodyCoords, otherSnakeCoords, expected, name);
    }
    
    #endregion
    
    #region Test Griglia 19x19
    
    [TestMethod]
    [DataRow(19u, 19u, 9u, 9u, new uint[] { }, new uint[] { }, new uint[] { }, ALL_DIRECTIONS,  "19x19 - Centro libero")]
    [DataRow(19u, 19u, 0u, 0u, new uint[] { }, new uint[] { }, new uint[] { }, MonteCarlo.DOWN | MonteCarlo.RIGHT,  "19x19 - Angolo top-MonteCarlo.LEFT")]
    [DataRow(19u, 19u, 18u, 0u, new uint[] { }, new uint[] { }, new uint[] { }, MonteCarlo.DOWN | MonteCarlo.LEFT,  "19x19 - Angolo top-MonteCarlo.RIGHT")]
    [DataRow(19u, 19u, 0u, 18u, new uint[] { }, new uint[] { }, new uint[] { }, MonteCarlo.UP | MonteCarlo.RIGHT,  "19x19 - Angolo bottom-MonteCarlo.LEFT")]
    [DataRow(19u, 19u, 18u, 18u, new uint[] { }, new uint[] { }, new uint[] { }, MonteCarlo.UP | MonteCarlo.LEFT,  "19x19 - Angolo bottom-MonteCarlo.RIGHT")]
    [DataRow(19u, 19u, 9u, 0u, new uint[] { }, new uint[] { }, new uint[] { }, MonteCarlo.DOWN | MonteCarlo.LEFT | MonteCarlo.RIGHT,  "19x19 - Bordo sMonteCarlo.UPeriore")]
    [DataRow(19u, 19u, 9u, 18u, new uint[] { }, new uint[] { }, new uint[] { }, MonteCarlo.UP | MonteCarlo.LEFT | MonteCarlo.RIGHT,  "19x19 - Bordo inferiore")]
    [DataRow(19u, 19u, 0u, 9u, new uint[] { }, new uint[] { }, new uint[] { }, MonteCarlo.UP | MonteCarlo.DOWN | MonteCarlo.RIGHT,  "19x19 - Bordo sinistro")]
    [DataRow(19u, 19u, 18u, 9u, new uint[] { }, new uint[] { }, new uint[] { }, MonteCarlo.UP | MonteCarlo.DOWN | MonteCarlo.LEFT,  "19x19 - Bordo destro")]
    public void Test_19x19_PositionBoundaries(uint width, uint height, uint headX, uint headY, uint[] hazardCoords, uint[] myBodyCoords, uint[] otherSnakeCoords, int expected, string name)
    {
        TestValidMoves(width, height, headX, headY, hazardCoords, myBodyCoords, otherSnakeCoords, expected, name);
    }
    
    [TestMethod]
    [DataRow(19u, 19u, 9u, 9u, new uint[] { }, new[] { 9u, 9u, 8u, 9u }, new uint[] { }, MonteCarlo.UP | MonteCarlo.DOWN | MonteCarlo.RIGHT,  "19x19 - Corpo a sinistra")]
    [DataRow(19u, 19u, 9u, 9u, new uint[] { }, new[] { 9u, 9u, 10u, 9u }, new uint[] { }, MonteCarlo.UP | MonteCarlo.DOWN | MonteCarlo.LEFT,  "19x19 - Corpo a destra")]
    [DataRow(19u, 19u, 9u, 9u, new uint[] { }, new[] { 9u, 9u, 9u, 8u }, new uint[] { }, MonteCarlo.DOWN | MonteCarlo.LEFT | MonteCarlo.RIGHT,  "19x19 - Corpo sopra")]
    [DataRow(19u, 19u, 9u, 9u, new uint[] { }, new[] { 9u, 9u, 9u, 10u }, new uint[] { }, MonteCarlo.UP | MonteCarlo.LEFT | MonteCarlo.RIGHT,  "19x19 - Corpo sotto")]
    [DataRow(19u, 19u, 9u, 9u, new uint[] { }, new[] { 9u, 9u, 8u, 9u, 10u, 9u, 9u, 8u, 9u, 10u }, new uint[] { }, 0,  "19x19 - Corpo tutte direzioni")]
    public void Test_19x19_MyBodyCollisions(uint width, uint height, uint headX, uint headY, uint[] hazardCoords, uint[] myBodyCoords, uint[] otherSnakeCoords, int expected, string name)
    {
        TestValidMoves(width, height, headX, headY, hazardCoords, myBodyCoords, otherSnakeCoords, expected, name);
    }
    
    [TestMethod]
    [DataRow(19u, 19u, 9u, 9u, new uint[] { }, new uint[] { }, new[] { 8u, 9u }, MonteCarlo.UP | MonteCarlo.DOWN | MonteCarlo.RIGHT,  "19x19 - Nemico a sinistra")]
    [DataRow(19u, 19u, 9u, 9u, new uint[] { }, new uint[] { }, new[] { 10u, 9u }, MonteCarlo.UP | MonteCarlo.DOWN | MonteCarlo.LEFT,  "19x19 - Nemico a destra")]
    [DataRow(19u, 19u, 9u, 9u, new uint[] { }, new uint[] { }, new[] { 9u, 8u }, MonteCarlo.DOWN | MonteCarlo.LEFT | MonteCarlo.RIGHT,  "19x19 - Nemico sopra")]
    [DataRow(19u, 19u, 9u, 9u, new uint[] { }, new uint[] { }, new[] { 9u, 10u }, MonteCarlo.UP | MonteCarlo.LEFT | MonteCarlo.RIGHT,  "19x19 - Nemico sotto")]
    public void Test_19x19_EnemyCollisions(uint width, uint height, uint headX, uint headY, uint[] hazardCoords, uint[] myBodyCoords, uint[] otherSnakeCoords, int expected, string name)
    {
        TestValidMoves(width, height, headX, headY, hazardCoords, myBodyCoords, otherSnakeCoords, expected, name);
    }
    
    [TestMethod]
    [DataRow(19u, 19u, 9u, 9u, new[] { 8u, 9u }, new uint[] { }, new uint[] { }, MonteCarlo.UP | MonteCarlo.DOWN | MonteCarlo.RIGHT, "19x19 - Hazard a sinistra")]
    [DataRow(19u, 19u, 9u, 9u, new[] { 10u, 9u }, new uint[] { }, new uint[] { }, MonteCarlo.UP | MonteCarlo.DOWN | MonteCarlo.LEFT,  "19x19 - Hazard a destra")]
    [DataRow(19u, 19u, 9u, 9u, new[] { 9u, 8u }, new uint[] { }, new uint[] { }, MonteCarlo.DOWN | MonteCarlo.LEFT | MonteCarlo.RIGHT,  "19x19 - Hazard sopra")]
    [DataRow(19u, 19u, 9u, 9u, new[] { 9u, 10u }, new uint[] { }, new uint[] { }, MonteCarlo.UP | MonteCarlo.LEFT | MonteCarlo.RIGHT,  "19x19 - Hazard sotto")]
    public void Test_19x19_HazardCollisions(uint width, uint height, uint headX, uint headY, uint[] hazardCoords, uint[] myBodyCoords, uint[] otherSnakeCoords, int expected, string name)
    {
        TestValidMoves(width, height, headX, headY, hazardCoords, myBodyCoords, otherSnakeCoords, expected, name);
    }
    
    #endregion
    
    #region Test Scenari Complessi
    
    [TestMethod]
    [DataRow(11u, 11u, 1u, 1u, new[] { 0u, 1u }, new[] { 1u, 1u, 1u, 0u }, new[] { 2u, 1u }, MonteCarlo.DOWN,  "11x11 - Angolo con ostacoli multipli")]
    [DataRow(19u, 19u, 1u, 1u, new[] { 0u, 1u }, new[] { 1u, 1u, 1u, 0u }, new[] { 2u, 1u }, MonteCarlo.DOWN,  "19x19 - Angolo con ostacoli multipli")]
    [DataRow(11u, 11u, 5u, 5u, new[] { 4u, 5u }, new[] { 5u, 5u, 6u, 5u }, new[] { 5u, 4u }, MonteCarlo.DOWN,  "11x11 - Solo MonteCarlo.DOWN disponibile")]
    [DataRow(19u, 19u, 9u, 9u, new[] { 8u, 9u }, new[] { 9u, 9u, 10u, 9u }, new[] { 9u, 8u }, MonteCarlo.DOWN,  "19x19 - Solo MonteCarlo.DOWN disponibile")]
    [DataRow(11u, 11u, 5u, 5u, new[] { 4u, 5u, 6u, 5u, 5u, 4u, 5u, 6u }, new uint[] { }, new uint[] { }, 0,  "11x11 - Nessuna mossa valida")]
    [DataRow(19u, 19u, 9u, 9u, new[] { 8u, 9u, 10u, 9u, 9u, 8u, 9u, 10u }, new uint[] { }, new uint[] { }, 0,  "19x19 - Nessuna mossa valida")]
    public void Test_ComplexScenarios(uint width, uint height, uint headX, uint headY, uint[] hazardCoords, uint[] myBodyCoords, uint[] otherSnakeCoords, int expected, string name)
    {
        TestValidMoves(width, height, headX, headY, hazardCoords, myBodyCoords, otherSnakeCoords, expected, name);
    }
    
    #endregion
    
    #region Test Edge Cases
    
    [TestMethod]
    [DataRow(11u, 11u, 1u, 0u, new uint[] { }, new uint[] { }, new uint[] { }, MonteCarlo.DOWN | MonteCarlo.LEFT | MonteCarlo.RIGHT,  "11x11 - Vicino al bordo")]
    [DataRow(11u, 11u, 9u, 0u, new uint[] { }, new uint[] { }, new uint[] { }, MonteCarlo.DOWN | MonteCarlo.LEFT | MonteCarlo.RIGHT,  "11x11 - Vicino al bordo opposto")]
    [DataRow(19u, 19u, 1u, 0u, new uint[] { }, new uint[] { }, new uint[] { }, MonteCarlo.DOWN | MonteCarlo.LEFT | MonteCarlo.RIGHT,  "19x19 - Vicino al bordo")]
    [DataRow(19u, 19u, 17u, 0u, new uint[] { }, new uint[] { }, new uint[] { }, MonteCarlo.DOWN | MonteCarlo.LEFT | MonteCarlo.RIGHT,  "19x19 - Vicino al bordo opposto")]
    [DataRow(11u, 11u, 5u, 1u, new uint[] { }, new uint[] { }, new uint[] { }, MonteCarlo.UP | MonteCarlo.DOWN | MonteCarlo.LEFT | MonteCarlo.RIGHT,  "11x11 - Quasi al bordo")]
    [DataRow(19u, 19u, 9u, 1u, new uint[] { }, new uint[] { }, new uint[] { }, MonteCarlo.UP | MonteCarlo.DOWN | MonteCarlo.LEFT | MonteCarlo.RIGHT,  "19x19 - Quasi al bordo")]
    public void Test_EdgeCases(uint width, uint height, uint headX, uint headY, uint[] hazardCoords, uint[] myBodyCoords, uint[] otherSnakeCoords, int expected, string name)
    {
        TestValidMoves(width, height, headX, headY, hazardCoords, myBodyCoords, otherSnakeCoords, expected, name);
    }
    
    #endregion
    
    #region Test Performance Corner Cases
    
    [TestMethod]
    [DataRow(11u, 11u, 0u, 5u, new uint[] { }, new[] { 0u, 5u, 0u, 4u, 0u, 6u, 1u, 5u }, new uint[] { }, 0,  "11x11 - Serpente lungo corpo")]
    [DataRow(19u, 19u, 0u, 9u, new uint[] { }, new[] { 0u, 9u, 0u, 8u, 0u, 10u, 1u, 9u }, new uint[] { }, 0,  "19x19 - Serpente lungo corpo")]
    [DataRow(11u, 11u, 5u, 5u, new uint[] { }, new uint[] { }, new[] { 4u, 4u, 4u, 5u, 4u, 6u, 5u, 4u, 5u, 6u, 6u, 4u, 6u, 5u, 6u, 6u }, MonteCarlo.UP | MonteCarlo.DOWN | MonteCarlo.LEFT | MonteCarlo.RIGHT,  "11x11 - Nemico circondato")]
    [DataRow(19u, 19u, 9u, 9u, new uint[] { }, new uint[] { }, new[] { 8u, 8u, 8u, 9u, 8u, 10u, 9u, 8u, 9u, 10u, 10u, 8u, 10u, 9u, 10u, 10u }, MonteCarlo.UP | MonteCarlo.DOWN | MonteCarlo.LEFT | MonteCarlo.RIGHT,  "19x19 - Nemico circondato")]
    public void Test_PerformanceCornerCases(uint width, uint height, uint headX, uint headY, uint[] hazardCoords, uint[] myBodyCoords, uint[] otherSnakeCoords, int expected, string name)
    {
        TestValidMoves(width, height, headX, headY, hazardCoords, myBodyCoords, otherSnakeCoords, expected, name);
    }
    
    #endregion
    
    #region Test per Coda del Serpente
    
    [TestMethod]
    [DataRow(11u, 11u, 5u, 5u, new uint[] { }, new[] { 5u, 5u, 4u, 5u, 3u, 5u }, new uint[] { }, MonteCarlo.UP | MonteCarlo.DOWN | MonteCarlo.RIGHT,  "11x11 - Coda liberabile")]
    [DataRow(11u, 11u, 5u, 5u, new uint[] { }, new[] { 5u, 5u, 4u, 5u, 3u, 5u }, new uint[] { }, MonteCarlo.UP | MonteCarlo.DOWN | MonteCarlo.LEFT | MonteCarlo.RIGHT,  "11x11 - Movimento su coda permesso")]
    [DataRow(19u, 19u, 9u, 9u, new uint[] { }, new[] { 9u, 9u, 8u, 9u, 7u, 9u, 6u, 9u }, new uint[] { }, MonteCarlo.UP | MonteCarlo.DOWN | MonteCarlo.LEFT | MonteCarlo.RIGHT,  "19x19 - Serpente lungo con coda")]
    public void Test_TailMovement(uint width, uint height, uint headX, uint headY, uint[] hazardCoords, uint[] myBodyCoords, uint[] otherSnakeCoords, int expected, string name)
    {
        TestValidMoves(width, height, headX, headY, hazardCoords, myBodyCoords, otherSnakeCoords, expected, name);
    }

    #endregion

    private static void TestValidMoves(uint width, uint height, uint headX, uint headY, uint[] hazardCoords, uint[] myBodyCoords, uint[] otherSnakeCoords, int expected, string testName)
    {
        // Arrange
        const string myId = "me";

        var myBody = ToPoints(myBodyCoords).ToArray();
        var hazards = ToPoints(hazardCoords).ToArray();
        Snake[] snakes = otherSnakeCoords.Length > 0
            ? [new Snake { id = "enemy", body = ToPoints(otherSnakeCoords).ToArray(), head = new Point(otherSnakeCoords[0], otherSnakeCoords[1]) }]
            : [];

        var myBodyLength = myBody.Length;
        var hazardCount = hazards.Length;
        var snakeCount = snakes.Length;

        Console.WriteLine("=== DEBUG TESTS ===");
        
        // Test 1: Centro libero
        GridDebugger.PrintGrid(width, height, headX, headY, myBody, hazards, snakes, expected, testName);
        
        // Act
        var result = GetValidMovesLightSpeedB.GetValidMovesLightSpeed(
            width, height, myId,
            myBody, myBodyLength,
            headX, headY,
            hazards, hazardCount,
            snakes, snakeCount,
            false // eat
        );
        
        if(result == expected)
        {
            Console.WriteLine("🎉🎉🎉🎉🎉🎉🎉🎉🎉🎉🎉🎉🎉🎉🎉🎉🎉🎉🎉🎉🎉🎉🎉");
            Console.WriteLine($"✅ Test '{testName}' PASSATO!");
            Console.WriteLine($"Atteso: {expected}");
            Console.WriteLine($"Ottenuto: {result}");
            Console.WriteLine("🎉🎉🎉🎉🎉🎉🎉🎉🎉🎉🎉🎉🎉🎉🎉🎉🎉🎉🎉🎉🎉🎉🎉");
        }
        else
        {
            Console.WriteLine("💀💀💀💀💀💀💀💀💀💀💀💀💀💀💀💀💀💀💀💀💀💀💀");
            Console.WriteLine($"❌ Test '{testName}' FALLITO!");
            Console.WriteLine($"Atteso: {expected}");
            Console.WriteLine($"Ottenuto: {result}");
            Console.WriteLine("💀💀💀💀💀💀💀💀💀💀💀💀💀💀💀💀💀💀💀💀💀💀💀");
        }
        
        Assert.AreEqual(expected, result, $"❌ Test fallito per posizione ({headX},{headY}) su griglia {width}x{height}.\nAtteso: {expected}, Ottenuto: {result}");
    }

    private static IEnumerable<Point> ToPoints(uint[] coords)
    {
        for (var i = 0; i < coords.Length; i += 2) yield return new Point(coords[i], coords[i + 1]);
    }
}