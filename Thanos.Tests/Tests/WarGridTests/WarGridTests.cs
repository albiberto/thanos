// using Thanos.SourceGen; // Assuming Coordinate is in this namespace
// using Thanos.War.Grid;
//
// namespace Thanos.Tests.Tests.WarGridTests;
//
// /// <summary>
// /// Contains all unit tests for the WarGrid ref struct.
// /// These tests verify the constructors, the state-querying methods (IsOccupied, IsFood, IsHazard),
// /// and helper methods, ensuring correct behavior and memory mapping.
// /// </summary>
// [TestFixture]
// public class WarGridTests
// {
//     // =================================================================
//     // Test Harness (Memory Management)
//     // =================================================================
//
//     /// <summary>
//     /// A container for the raw memory buffers required to host a WarGrid instance.
//     /// </summary>
//     private struct WarGridTestContext
//     {
//         public ulong[] FoodBuffer;
//         public ulong[] HazardsBuffer;
//         public ulong[] SnakesBuffer;
//     }
//
//     /// <summary>
//     /// Allocates the raw, zeroed-out memory buffers needed for a WarGrid.
//     /// </summary>
//     private WarGridTestContext CreateTestContext(int width, int height)
//     {
//         int area = width * height;
//         // Calculates the number of ulongs needed to store 'area' bits.
//         int bufferSize = (area + 63) / 64; 
//
//         return new WarGridTestContext
//         {
//             FoodBuffer = new ulong[bufferSize],
//             HazardsBuffer = new ulong[bufferSize],
//             SnakesBuffer = new ulong[bufferSize]
//         };
//     }
//     
//     // =================================================================
//     // Constructor Tests
//     // =================================================================
//
//     [Test(Description = "Ensures the base constructor correctly initializes grid dimensions and attaches the bitboards.")]
//     public void BaseConstructor_ShouldInitializePropertiesAndBitboards()
//     {
//         // Arrange
//         const int width = 11;
//         const int height = 11;
//         var context = CreateTestContext(width, height);
//         
//         // Act
//         var grid = new WarGrid(width, height, width * height, context.FoodBuffer, context.HazardsBuffer, context.SnakesBuffer);
//         
//         // Assert
//             Assert.That(grid.Width, Is.EqualTo(width), "Width should be initialized correctly.");
//             Assert.That(grid.Height, Is.EqualTo(height), "Height should be initialized correctly.");
//             Assert.That(grid.Area, Is.EqualTo(width * height), "Area should be calculated and stored correctly.");
//             
//             // Verify that the bitboards are "live" and connected to the correct underlying buffer.
//             grid.Food.Set(10);
//             Assert.That(context.FoodBuffer[0], Is.Not.EqualTo(0), "Modifying grid.Food should modify the underlying FoodBuffer.");
//     }
//     
//     [Test(Description = "Ensures the initializing constructor correctly populates the bitboards from coordinate spans.")]
//     public void InitializingConstructor_ShouldSetInitialFoodAndHazards()
//     {
//         // Arrange
//         const int width = 11;
//         const int height = 11;
//         var context = CreateTestContext(width, height);
//         
//         var foodCoords = new[] { new Coordinate(1, 1), new Coordinate(2, 2) };
//         var hazardCoords = new[] { new Coordinate(3, 3) };
//
//         // Act
//         var grid = new WarGrid(width, height, width * height, context.FoodBuffer, context.HazardsBuffer, context.SnakesBuffer, foodCoords, hazardCoords);
//         
//         // Assert
//             // Verify food positions
//             Assert.That(grid.IsFood(grid.To1D(foodCoords[0])), Is.True, "First food coordinate should be set.");
//             Assert.That(grid.IsFood(grid.To1D(foodCoords[1])), Is.True, "Second food coordinate should be set.");
//             Assert.That(grid.IsFood(grid.To1D(new Coordinate(4,4))), Is.False, "An empty coordinate should not be food.");
//             
//             // Verify hazard positions
//             Assert.That(grid.IsHazard(grid.To1D(hazardCoords[0])), Is.True, "Hazard coordinate should be set.");
//             Assert.That(grid.IsHazard(grid.To1D(new Coordinate(5,5))), Is.False, "An empty coordinate should not be a hazard.");
//             
//             // Verify snakes bitboard is untouched
//             Assert.That(grid.IsOccupied(grid.To1D(new Coordinate(1,1))), Is.False, "Snakes bitboard should be empty.");
//     }
//
//     // =================================================================
//     // Read Method Tests
//     // =================================================================
//
//     [Test(Description = "Verifies that IsFood, IsHazard, and IsOccupied methods accurately report the state of various grid positions.")]
//     public void ReadMethods_ShouldCorrectlyReportStateOfPositions()
//     {
//         // Arrange
//         const int width = 11;
//         const int height = 11;
//         var context = CreateTestContext(width, height);
//         var grid = new WarGrid(width, height, width * height, context.FoodBuffer, context.HazardsBuffer, context.SnakesBuffer);
//         
//         // Manually set the state for distinct positions
//         var foodCoord = new Coordinate(5, 0);
//         var hazardCoord = new Coordinate(0, 5);
//         var snakeCoord = new Coordinate(5, 5);
//         var emptyCoord = new Coordinate(1, 1);
//         
//         grid.Food.Set(grid.To1D(foodCoord));
//         grid.Hazards.Set(grid.To1D(hazardCoord));
//         grid.Snakes.Set(grid.To1D(snakeCoord));
//
//         // Act & Assert
//             // Check the food cell
//             Assert.That(grid.IsFood(grid.To1D(foodCoord)), Is.True, "Food cell should be food.");
//             Assert.That(grid.IsHazard(grid.To1D(foodCoord)), Is.False, "Food cell should not be a hazard.");
//             Assert.That(grid.IsOccupied(grid.To1D(foodCoord)), Is.False, "Food cell should not be occupied.");
//             
//             // Check the hazard cell
//             Assert.That(grid.IsHazard(grid.To1D(hazardCoord)), Is.True, "Hazard cell should be a hazard.");
//             Assert.That(grid.IsOccupied(grid.To1D(hazardCoord)), Is.False, "Hazard cell should not be occupied.");
//
//             // Check the snake cell
//             Assert.That(grid.IsOccupied(grid.To1D(snakeCoord)), Is.True, "Snake cell should be occupied.");
//             Assert.That(grid.IsFood(grid.To1D(snakeCoord)), Is.False, "Snake cell should not be food.");
//
//             // Check the empty cell
//             Assert.That(grid.IsFood(grid.To1D(emptyCoord)), Is.False, "Empty cell should not be food.");
//             Assert.That(grid.IsHazard(grid.To1D(emptyCoord)), Is.False, "Empty cell should not be a hazard.");
//             Assert.That(grid.IsOccupied(grid.To1D(emptyCoord)), Is.False, "Empty cell should not be occupied.");
//     }
//
//     [Test(Description = "Verifies the special-case rule that IsOccupied returns true for ushort.MaxValue.")]
//     public void IsOccupied_WithMaxValue_ShouldReturnTrue()
//     {
//         // Arrange
//         var context = CreateTestContext(5, 5);
//         var grid = new WarGrid(5, 5, 25, context.FoodBuffer, context.HazardsBuffer, context.SnakesBuffer);
//         
//         // Act
//         var result = grid.IsOccupied(ushort.MaxValue);
//         
//         // Assert
//         Assert.That(result, Is.True, "IsOccupied should always return true for ushort.MaxValue, representing an invalid/off-board position.");
//     }
//
//     // =================================================================
//     // Helper Method Tests
//     // =================================================================
//     
//     // =================================================================
// // Helper Method Tests
// // =================================================================
//
//     [TestCase(0, 0, 11, ExpectedResult = (ushort)0, TestName = "To1D (Static): Origin (0,0)")]
//     [TestCase(10, 0, 11, ExpectedResult = (ushort)10, TestName = "To1D (Static): End of the first row")]
//     [TestCase(0, 1, 11, ExpectedResult = (ushort)11, TestName = "To1D (Static): Start of the second row")]
//     [TestCase(5, 5, 11, ExpectedResult = (ushort)60, TestName = "To1D (Static): A middle point")]
//     [TestCase(10, 10, 11, ExpectedResult = (ushort)120, TestName = "To1D (Static): Bottom-right corner (boundary case)")]
//     [Test(Description = "Ensures the static To1D helper method correctly converts 2D coordinates to a 1D index.")]
//     public ushort Static_To1D_ShouldCorrectlyConvertCoordinates(int x, int y, int width)
//     {
//         // Arrange
//         var coord = new Coordinate((ushort)x, (ushort)y);
//     
//         // Act & Assert
//         return WarGrid.To1D(in coord, width);
//     }
//
//     [Test(Description = "Ensures the instance To1D method correctly uses the grid's internal width.")]
//     public void Instance_To1D_ShouldUseInternalWidthCorrectly()
//     {
//         // Arrange
//         const int width = 11;
//         const int height = 11;
//         // L'harness non è strettamente necessario qui, ma lo usiamo per coerenza.
//         var context = CreateTestContext(width, height); 
//         var grid = new WarGrid(width, height, width * height, context.FoodBuffer, context.HazardsBuffer, context.SnakesBuffer);
//     
//         var coord = new Coordinate(5, 5);
//         ushort expectedPosition = 60; // 5 * 11 + 5
//
//         // Act
//         var actualPosition = grid.To1D(in coord);
//
//         // Assert
//         Assert.That(actualPosition, Is.EqualTo(expectedPosition), "The instance method should produce the correct 1D index based on grid.Width.");
//     }
// }