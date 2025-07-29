using Thanos.Parsers;
using Thanos.Tests.Support;

namespace Thanos.Tests;

/// <summary>
/// Test suite per UltraFastParser usando NUnit
/// </summary>
[TestFixture]
public unsafe class UltraFastParserTests
{
    private TestDataProvider _testDataProvider;

    [OneTimeSetUp]
    public void OneTimeSetUp() => _testDataProvider = new TestDataProvider();
    
    [Test]
    public void Parse_BasicGameState_ShouldExtractCorrectValues()
    {
        // Arrange
        var (bytes, width, height) = _testDataProvider["basic_game"];
        var state = Initialize(width, height);

        // Act
        UltraFastParser.Parse(bytes, state);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(state->Turn, Is.EqualTo(42));
            Assert.That(state->Width, Is.EqualTo(11));
            Assert.That(state->Height, Is.EqualTo(11));
            Assert.That(state->TotalCells, Is.EqualTo(121));
            Assert.That(state->SnakeCount, Is.EqualTo(1));
            Assert.That(state->FoodCount, Is.EqualTo(1));
            Assert.That(state->Snakes[0].Health, Is.EqualTo(100));
            Assert.That(state->Snakes[0].Head, Is.EqualTo(GridMath.ToIndex(1, 1, 11)));
            Assert.That(state->FoodPositions[0], Is.EqualTo(GridMath.ToIndex(5, 5, 11)));
        });
    }

    [Test]
    public void Parse_MultipleSnakes_ShouldHandleAllSnakes()
    {
        // Arrange
        var (bytes, width, height) = _testDataProvider["multiple_snakes"];
        var state = Initialize(width, height);
        
        // Act
        UltraFastParser.Parse(bytes, state);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(state->SnakeCount, Is.EqualTo(3));
            
            Assert.That(state->Snakes[0].Health, Is.EqualTo(90));
            Assert.That(state->Snakes[0].Head, Is.EqualTo(GridMath.ToIndex(0, 0, 7)));
            
            Assert.That(state->Snakes[1].Health, Is.EqualTo(85));
            Assert.That(state->Snakes[1].Head, Is.EqualTo(GridMath.ToIndex(6, 6, 7)));
            
            Assert.That(state->Snakes[2].Health, Is.EqualTo(75));
            Assert.That(state->Snakes[2].Head, Is.EqualTo(GridMath.ToIndex(3, 3, 7)));
        });
    }

    [Test]
    public void Parse_SnakeWithLongBody_ShouldParseAllBodySegments()
    {
        // Arrange
        var (bytes, width, height) = _testDataProvider["snake_with_body"];
        var state = Initialize(width, height);

        // Act
        UltraFastParser.Parse(bytes, state);

        // Assert
        Assert.That(state->Snakes[0].Length, Is.EqualTo(5));
        Assert.That(state->Snakes[0].Head, Is.EqualTo(GridMath.ToIndex(5, 5, 11)));

        var expectedBodyPositions = new[]
        {
            GridMath.ToIndex(5, 5, 11),
            GridMath.ToIndex(5, 4, 11),
            GridMath.ToIndex(5, 3, 11),
            GridMath.ToIndex(4, 3, 11),
            GridMath.ToIndex(3, 3, 11)
        };

        for (var i = 0; i < 5; i++) Assert.That(state->Snakes[0].Body[i], Is.EqualTo(expectedBodyPositions[i]), $"Body segment {i} should be at correct position");
    }

    [Test]
    public void Parse_MultipleFoodItems_ShouldParseAllFood()
    {
        // Arrange
        var (bytes, width, height) = _testDataProvider["multiple_food"];
        var state = Initialize(width, height);

        // Act
        UltraFastParser.Parse(bytes, state);

        // Assert
        Assert.That(state->FoodCount, Is.EqualTo(5));

        var expectedFoodPositions = new[]
        {
            GridMath.ToIndex(1, 1, 11),
            GridMath.ToIndex(9, 1, 11),
            GridMath.ToIndex(1, 9, 11),
            GridMath.ToIndex(9, 9, 11),
            GridMath.ToIndex(5, 5, 11)
        };

        for (var i = 0; i < 5; i++) Assert.That(state->FoodPositions[i], Is.EqualTo(expectedFoodPositions[i]), $"Food item {i} should be at correct position");
    }

    [Test]
    public void Parse_LargeGrid_ShouldHandleBiggerBoard()
    {
        // Arrange
        var (bytes, width, height) = _testDataProvider["large_grid"];
        var state = Initialize(width, height);
        
        // Act
        UltraFastParser.Parse(bytes, state);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(state->Width, Is.EqualTo(25));
            Assert.That(state->Height, Is.EqualTo(25));
            Assert.That(state->TotalCells, Is.EqualTo(625));
            Assert.That(state->Snakes[0].Head, Is.EqualTo(GridMath.ToIndex(12, 12, 25)));
            Assert.That(state->FoodPositions[0], Is.EqualTo(GridMath.ToIndex(24, 24, 25)));
        });
    }

    [Test]
    public void Parse_EdgeCaseValues_ShouldHandleMinimalValues()
    {
        // Arrange
        var (bytes, width, height) = _testDataProvider["edge_cases"];
        var state = Initialize(width, height);
        
        // Act
        UltraFastParser.Parse(bytes, state);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(state->Turn, Is.EqualTo(0));
            Assert.That(state->Width, Is.EqualTo(1));
            Assert.That(state->Height, Is.EqualTo(1));
            Assert.That(state->TotalCells, Is.EqualTo(1));
            Assert.That(state->Snakes[0].Health, Is.EqualTo(1));
            Assert.That(state->Snakes[0].Head, Is.EqualTo(0));
            Assert.That(state->FoodPositions[0], Is.EqualTo(0));
        });
    }

    [Test]
    public void Parse_ComplexGame_ShouldHandleRealisticScenario()
    {
        // Arrange
        var (bytes, width, height) = _testDataProvider["complex_game"];
        var state = Initialize(width, height);

        // Act
        UltraFastParser.Parse(bytes, state);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(state->Turn, Is.EqualTo(150));
            Assert.That(state->SnakeCount, Is.EqualTo(2));
            Assert.That(state->FoodCount, Is.EqualTo(2));
            
            Assert.That(state->Snakes[0].Length, Is.EqualTo(5));
            Assert.That(state->Snakes[0].Health, Is.EqualTo(85));
            
            Assert.That(state->Snakes[1].Length, Is.EqualTo(3));
            Assert.That(state->Snakes[1].Health, Is.EqualTo(70));
        });
    }

    [Test]
    public void Parse_MalformedJson_ShouldTolerateExtraWhitespace()
    {
        // Arrange
        var (bytes, width, height) = _testDataProvider["malformed_tolerant"];
        var state = Initialize(width, height);
        
        // Act
        UltraFastParser.Parse(bytes, state);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(state->Turn, Is.EqualTo(50));
            Assert.That(state->Width, Is.EqualTo(11));
            Assert.That(state->Height, Is.EqualTo(11));
            Assert.That(state->Snakes[0].Health, Is.EqualTo(99));
            Assert.That(state->FoodPositions[0], Is.EqualTo(GridMath.ToIndex(3, 7, 11)));
        });
    }

    [Test]
    public void Parse_EmptyArrays_ShouldHandleNoFoodOrSnakes()
    {
        // Arrange
        var (bytes, width, height) = _testDataProvider["empty_arrays"];
        var state = Initialize(width, height);

        // Act
        UltraFastParser.Parse(bytes, state);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(state->SnakeCount, Is.EqualTo(0));
            Assert.That(state->FoodCount, Is.EqualTo(0));
            Assert.That(state->Width, Is.EqualTo(11));
            Assert.That(state->Height, Is.EqualTo(11));
        });
    }
    
    private static GameState* Initialize(byte width = 11, byte height = 11)
    {
        GameManager.Initialize(width, height);
        return GameManager.State;
    }
}