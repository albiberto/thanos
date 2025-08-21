using Thanos.SourceGen;
using Thanos.War.Grid;

namespace Thanos.Tests.Tests.WarGridTests
{
    [TestFixture]
    public class WarGridTests
    {
        // --- Test Harness per la Memoria ---

        private struct WarGridTestContext
        {
            public ulong[] FoodBuffer;
            public ulong[] HazardsBuffer;
            public ulong[] SnakesBuffer;
        }

        private WarGridTestContext CreateTestContext(int width, int height)
        {
            int area = width * height;
            // Calcola la dimensione del buffer necessaria per contenere 'area' bit.
            int bufferSize = (area + 63) / 64; 

            return new WarGridTestContext
            {
                FoodBuffer = new ulong[bufferSize],
                HazardsBuffer = new ulong[bufferSize],
                SnakesBuffer = new ulong[bufferSize]
            };
        }
    
        // --- Test dei Costruttori ---

        [Test(Description = "Verifica che il costruttore base inizializzi le proprietà e agganci i bitboard.")]
        public void BaseConstructor_ShouldInitializePropertiesAndBitboards()
        {
            // Arrange
            const int width = 11;
            const int height = 11;
            var context = CreateTestContext(width, height);
        
            // Act
            var grid = new WarGrid(width, height, width * height, context.FoodBuffer, context.HazardsBuffer, context.SnakesBuffer);
        
            // Assert
            Assert.That(grid.Width, Is.EqualTo(width));
            Assert.That(grid.Height, Is.EqualTo(height));
            Assert.That(grid.Area, Is.EqualTo(width * height));
            
            // Verifica che i bitboard siano "vivi" e connessi al buffer corretto
            Assert.That(grid.Food.IsSet(10), Is.False);
            grid.Food.Set(10);
            Assert.That(context.FoodBuffer[0], Is.Not.EqualTo(0));
        }
    
        [Test(Description = "Verifica che il costruttore con dati iniziali popoli correttamente i bitboard.")]
        public void InitializingConstructor_ShouldSetFoodAndHazards()
        {
            // Arrange
            const int width = 11;
            const int height = 11;
            var context = CreateTestContext(width, height);
        
            var foodCoords = new[] { new Coordinate(1, 1), new Coordinate(2, 2) };
            var hazardCoords = new[] { new Coordinate(3, 3) };

            // Act
            var grid = new WarGrid(width, height, width * height, context.FoodBuffer, context.HazardsBuffer, context.SnakesBuffer, foodCoords, hazardCoords);
        
            // Assert
            // Verifichiamo le posizioni del cibo
            Assert.That(grid.IsFood(grid.To1D(foodCoords[0])), Is.True);
            Assert.That(grid.IsFood(grid.To1D(foodCoords[1])), Is.True);
            Assert.That(grid.IsFood(grid.To1D(new Coordinate(4,4))), Is.False);
            
            // Verifichiamo le posizioni dei pericoli
            Assert.That(grid.IsHazard(grid.To1D(hazardCoords[0])), Is.True);
            Assert.That(grid.IsHazard(grid.To1D(new Coordinate(5,5))), Is.False);
            
            // Verifichiamo che i serpenti siano vuoti
            Assert.That(grid.IsOccupied(grid.To1D(new Coordinate(1,1))), Is.False);
        }

        // --- Test dei Metodi di Lettura ---

        [Test(Description = "Verifica che i metodi IsOccupied, IsFood e IsHazard leggano lo stato corretto.")]
        public void ReadMethods_ShouldCorrectlyReportStateOfPositions()
        {
            // Arrange
            const int width = 11;
            const int height = 11;
            var context = CreateTestContext(width, height);
            var grid = new WarGrid(width, height, width * height, context.FoodBuffer, context.HazardsBuffer, context.SnakesBuffer);
        
            // Impostiamo manualmente lo stato usando le coordinate e il metodo To1D
            var foodCoord = new Coordinate(5, 0);
            var hazardCoord = new Coordinate(0, 5);
            var snakeCoord = new Coordinate(5, 5);
            var emptyCoord = new Coordinate(1, 1);
        
            grid.Food.Set(grid.To1D(foodCoord));
            grid.Hazards.Set(grid.To1D(hazardCoord));
            grid.Snakes.Set(grid.To1D(snakeCoord));

            // Act & Assert
            // Controlla la cella con il cibo
            Assert.That(grid.IsFood(grid.To1D(foodCoord)), Is.True, "Cella cibo (IsFood)");
            Assert.That(grid.IsHazard(grid.To1D(foodCoord)), Is.False, "Cella cibo (IsHazard)");
            Assert.That(grid.IsOccupied(grid.To1D(foodCoord)), Is.False, "Cella cibo (IsOccupied)");
            
            // Controlla la cella con il pericolo
            Assert.That(grid.IsFood(grid.To1D(hazardCoord)), Is.False, "Cella pericolo (IsFood)");
            Assert.That(grid.IsHazard(grid.To1D(hazardCoord)), Is.True, "Cella pericolo (IsHazard)");
            Assert.That(grid.IsOccupied(grid.To1D(hazardCoord)), Is.False, "Cella pericolo (IsOccupied)");

            // Controlla la cella con il serpente
            Assert.That(grid.IsFood(grid.To1D(snakeCoord)), Is.False, "Cella serpente (IsFood)");
            Assert.That(grid.IsHazard(grid.To1D(snakeCoord)), Is.False, "Cella serpente (IsHazard)");
            Assert.That(grid.IsOccupied(grid.To1D(snakeCoord)), Is.True, "Cella serpente (IsOccupied)");

            // Controlla la cella vuota
            Assert.That(grid.IsFood(grid.To1D(emptyCoord)), Is.False, "Cella vuota (IsFood)");
            Assert.That(grid.IsHazard(grid.To1D(emptyCoord)), Is.False, "Cella vuota (IsHazard)");
            Assert.That(grid.IsOccupied(grid.To1D(emptyCoord)), Is.False, "Cella vuota (IsOccupied)");
        }

        [Test(Description = "Verifica il caso limite di IsOccupied con ushort.MaxValue.")]
        public void IsOccupied_WithMaxValue_ShouldReturnTrue()
        {
            // Arrange
            var context = CreateTestContext(5, 5);
            var grid = new WarGrid(5, 5, 25, context.FoodBuffer, context.HazardsBuffer, context.SnakesBuffer);
        
            // Act
            var result = grid.IsOccupied(ushort.MaxValue);
        
            // Assert
            Assert.That(result, Is.True);
        }

        // --- Test dei Metodi Helper ---
    
        [TestCase(0, 0, 11, ExpectedResult = 0, TestName = "To1D: Origine (0,0)")]
        [TestCase(10, 0, 11, ExpectedResult = 10, TestName = "To1D: Fine prima riga")]
        [TestCase(0, 1, 11, ExpectedResult = 11, TestName = "To1D: Inizio seconda riga")]
        [TestCase(5, 5, 11, ExpectedResult = 60, TestName = "To1D: Punto centrale (5*11 + 5)")]
        public ushort To1D_ShouldCorrectlyConvertCoordinates(int x, int y, int width)
        {
            // Arrange
            var coord = new Coordinate((ushort)x, (ushort)y);
        
            // Act & Assert
            return WarGrid.To1D(in coord, width);
        }
    }

// Assumiamo che la struct Coordinate esista in questo namespace per completezza del test.
// Nel tuo codice reale, questa probabilmente non è necessaria se il namespace è già referenziato.
}

namespace Thanos.SourceGen
{
    public struct Coordinate(ushort x, ushort y)
    {
        public ushort X = x;
        public ushort Y = y;
    }
}