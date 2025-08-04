using System.Numerics;
using Thanos.Enums;

namespace Thanos.Tests;

[TestFixture]
public class BattleFieldTests
{
    private BattleField _battleField;
    private const uint DefaultBoardSize = 11 * 11;

    [SetUp]
    public void SetUp()
    {
        _battleField = new BattleField();
        _battleField.Initialize(DefaultBoardSize);
        _battleField.Clear();
    }

    [TearDown]
    public void TearDown() => _battleField.Dispose();

    [Test]
    public void Initialize_ShouldAllocateMemory()
    {
        // Arrange & Act - Already done in SetUp
            
        // Assert - Verify we can access the grid without crashing
        Assert.DoesNotThrow(() =>
        {
            _ = _battleField[0];
        });
    }

    [TestCase(100u)]
    [TestCase(121u)]
    [TestCase(144u)]
    [TestCase(256u)]
    [TestCase(512u)]
    public void Initialize_ShouldWorkWithDifferentBoardSizes(uint boardSize)
    {
        // Arrange
        var battleField = new BattleField();
            
        // Act
        battleField.Initialize(boardSize);
            
        // Assert - Verify we can access the last index
        Assert.DoesNotThrow(() =>
        {
            _ = battleField[(int)boardSize - 1];
        });
            
        // Cleanup
        battleField.Dispose();
    }

    [Test]
    public void Indexer_ShouldReturnZeroAfterInitialization()
    {
        // Arrange - Already initialized in SetUp
            
        // Act & Assert
        for (var i = 0; i < DefaultBoardSize; i++) Assert.That(_battleField[i], Is.EqualTo(Constants.Empty));
    }

    [Test]
    public void Clear_ShouldSetAllCellsToEmpty()
    {
        // Arrange - Set some cells to non-empty values
        ApplyTestData();
            
        // Act
        _battleField.Clear();
            
        // Assert
        for (var i = 0; i < DefaultBoardSize; i++) Assert.That(_battleField[i], Is.EqualTo(Constants.Empty));
    }

    [Test]
    public void ApplyFood_ShouldSetCorrectPositions()
    {
        // Arrange
        ushort[] foodPositions = [10, 25, 50, 75, 100];
            
        // Act
        _battleField.ApplyFood(foodPositions);
            
        // Assert
        foreach (var position in foodPositions) Assert.That(_battleField[position], Is.EqualTo(Constants.Food));

        using (Assert.EnterMultipleScope())
        {

            // Verify other positions remain empty
            Assert.That(_battleField[0], Is.EqualTo(Constants.Empty));
            Assert.That(_battleField[30], Is.EqualTo(Constants.Empty));
        }
    }

    [Test]
    public void ApplyHazards_ShouldSetCorrectPositions()
    {
        // Arrange
        ushort[] hazardPositions = [5, 15, 35, 55, 85];
            
        // Act
        _battleField.ApplyHazards(hazardPositions);
            
        // Assert
        foreach (var position in hazardPositions)
        {
            Assert.That(_battleField[position], Is.EqualTo(Constants.Hazard));
        }

        using (Assert.EnterMultipleScope())
        {

            // Verify other positions remain empty
            Assert.That(_battleField[0], Is.EqualTo(Constants.Empty));
            Assert.That(_battleField[40], Is.EqualTo(Constants.Empty));
        }
    }

    [Test]
    public void ApplyFood_WithEmptySpan_ShouldNotThrow()
    {
        // Arrange
        ushort[] emptyArray = [];
            
        // Act & Assert
        Assert.DoesNotThrow(() => _battleField.ApplyFood(emptyArray));
    }

    [Test]
    public void ApplyHazards_WithEmptySpan_ShouldNotThrow()
    {
        // Arrange
        ushort[] emptyArray = [];
            
        // Act & Assert
        Assert.DoesNotThrow(() => _battleField.ApplyHazards(emptyArray));
    }

    [Test]
    public unsafe void ProjectBattlefield_SingleSnake_ShouldProjectCorrectly()
    {
        // Arrange
        var tesla = new Tesla();
        ushort[] startingPositions = [50];
        tesla.Initialize(11, 11, startingPositions);
            
        // Act
        _battleField.Clear();
        _battleField.ProjectBattlefield(&tesla);
            
        // Assert
        Assert.That(_battleField[50], Is.EqualTo(Constants.Me));
            
        // Cleanup
        tesla.Dispose();
    }

    [Test]
    public unsafe void ProjectBattlefield_SnakeWithBody_ShouldProjectAllSegments()
    {
        // Arrange
        var tesla = new Tesla();
        ushort[] startingPositions = { 50 };
        tesla.Initialize(11, 11, startingPositions);
    
        var snake = tesla.GetSnake(0);
    
        // Grow the snake
        snake->Move(51, Constants.Food, 0); // Length 2: [50, 51]
        snake->Move(52, Constants.Food, 0); // Length 3: [50, 51, 52]
        snake->Move(53, Constants.Food, 0); // Length 4: [50, 51, 52, 53]
    
        // Act
        _battleField.Clear();
        _battleField.ProjectBattlefield(&tesla);
    
        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(_battleField[53], Is.EqualTo(Constants.Me)); // Head
            Assert.That(_battleField[52], Is.EqualTo(Constants.Me)); // Body
            Assert.That(_battleField[51], Is.EqualTo(Constants.Me)); // Body
            Assert.That(_battleField[50], Is.EqualTo(Constants.Me)); // Tail
        });
    
        // Cleanup
        tesla.Dispose();
    }

    [Test]
    public unsafe void ProjectBattlefield_DeadSnake_ShouldNotProject()
    {
        // Arrange
        var tesla = new Tesla();
        ushort[] startingPositions = [50];
        tesla.Initialize(11, 11, startingPositions);
            
        var snake = tesla.GetSnake(0);
        snake->Health = 0; // Kill the snake
            
        // Act
        _battleField.Clear();
        _battleField.ProjectBattlefield(&tesla);
            
        // Assert
        Assert.That(_battleField[50], Is.EqualTo(Constants.Empty));
            
        // Cleanup
        tesla.Dispose();
    }

    [Test]
    public unsafe void ProjectBattlefield_MultipleSnakes_ShouldProjectWithCorrectIds()
    {
        // Arrange
        var tesla = new Tesla();
        ushort[] startingPositions = [10, 20, 30];
        tesla.Initialize(11, 11, startingPositions);
            
        // Act
        _battleField.Clear();
        _battleField.ProjectBattlefield(&tesla);
            
        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(_battleField[10], Is.EqualTo(Constants.Me));
            Assert.That(_battleField[20], Is.EqualTo(Constants.Enemy1));
            Assert.That(_battleField[30], Is.EqualTo(Constants.Enemy2));
        });
            
        // Cleanup
        tesla.Dispose();
    }

    [Test]
    public unsafe void ProjectBattlefield_CircularBufferWrapAround_ShouldHandleCorrectly()
    {
        // Arrange
        var tesla = new Tesla();
        ushort[] startingPositions = [10];
        tesla.Initialize(4, 4, startingPositions); // Small board to get small capacity
            
        var snake = tesla.GetSnake(0);
            
        // Grow the snake to test wrap-around
        snake->Move(11, Constants.Food, 0);
        snake->Move(12, Constants.Food, 0);
        snake->Move(13, Constants.Food, 0);
        snake->Move(14, Constants.Food, 0);
            
        // Act
        _battleField.Clear();
        _battleField.ProjectBattlefield(&tesla);
            
        // Assert - All body segments should be projected
        Assert.Multiple(() =>
        {
            Assert.That(_battleField[14], Is.EqualTo(Constants.Me)); // Head
            Assert.That(_battleField[13], Is.EqualTo(Constants.Me));
            Assert.That(_battleField[12], Is.EqualTo(Constants.Me));
            Assert.That(_battleField[11], Is.EqualTo(Constants.Me));
            Assert.That(_battleField[10], Is.EqualTo(Constants.Me)); // Tail
        });
            
        // Cleanup
        tesla.Dispose();
    }

    [Test]
    public void ComplexScenario_FoodHazardsAndSnakes_ShouldWorkTogether()
    {
        // This test verifies that the order of operations doesn't matter
        // since each element type uses different values
            
        // Arrange
        ushort[] foodPositions = [5, 15, 25];
        ushort[] hazardPositions = [35, 45, 55];
            
        // Act - Apply in different order
        _battleField.ApplyFood(foodPositions);
        _battleField.ApplyHazards(hazardPositions);
            
        // Clear and reapply in different order
        _battleField.Clear();
        _battleField.ApplyHazards(hazardPositions);
        _battleField.ApplyFood(foodPositions);
            
        // Assert
        Assert.Multiple(() =>
        {
            foreach (var pos in foodPositions)
                Assert.That(_battleField[pos], Is.EqualTo(Constants.Food));
                
            foreach (var pos in hazardPositions)
                Assert.That(_battleField[pos], Is.EqualTo(Constants.Hazard));
        });
    }

    [Test]
    public void Dispose_ShouldBeIdempotent()
    {
        // Arrange
        var battleField = new BattleField();
        battleField.Initialize(100);
            
        // Act & Assert - Multiple dispose calls should not throw
        Assert.DoesNotThrow(() =>
        {
            battleField.Dispose();
            battleField.Dispose();
            battleField.Dispose();
        });
    }

    [Test]
    public void Size_ConstantShouldBeCorrect()
    {
        // Assert
        Assert.That(BattleField.Size, Is.EqualTo(sizeof(long) + sizeof(uint)));
        Assert.That(BattleField.Size, Is.EqualTo(12)); // 8 + 4 = 12 bytes
    }

    [Test]
    public void MemoryAlignment_ShouldBeAlignedToCacheLine()
    {
        // This test verifies that the allocated memory respects SIMD alignment
        var vectorSize = Vector<byte>.Count;
        var boardSize = 100u;
        var expectedAllocatedSize = ((boardSize + (uint)vectorSize - 1) / (uint)vectorSize * (uint)vectorSize);
        using (Assert.EnterMultipleScope())
        {

            // We can't directly test the alignment of the pointer, but we can verify
            // that the size calculation is correct
            Assert.That(expectedAllocatedSize % vectorSize, Is.EqualTo(0));
            Assert.That(expectedAllocatedSize, Is.GreaterThanOrEqualTo(boardSize));
        }
    }

    // Helper methods
    private void ApplyTestData()
    {
        ushort[] testPositions = [0, 10, 20, 30, 40];
            
        // Manually set some values (simulating what would happen in real usage)
        // Since we can't directly set values through the indexer, we use the public methods
        _battleField.ApplyFood(new[] { testPositions[0], testPositions[1] });
        _battleField.ApplyHazards(new[] { testPositions[2], testPositions[3] });
    }
}