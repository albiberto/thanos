using Thanos.Shared;
using Thanos.SourceGen;
using static NUnit.Framework.Assert;

namespace Thanos.Tests.Shared;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class CoordinatesMatrixTests
{
    private static object[][] Dimensions => Support.Dimensions;

    [TestCaseSource(nameof(Dimensions))]
    public void Indexer_WhenIteratingEveryCell_ThenMatchesRowMajorLogic(byte width, byte height, ushort area)
    {
        // Arrange
        // Creiamo e popoliamo il buffer usando il Builder (che abbiamo già validato essere corretto)
        // O lo popoliamo manualmente per avere una "Doppia Validazione" (Oracle Testing).
        // Scegliamo la popolazione manuale nel test per essere indipendenti dal Builder.
        var buffer = new Coordinate[area];
        for (var i = 0; i < area; i++) 
        {
            buffer[i] = new Coordinate((byte)(i % width), (byte)(i / width));
        }

        var matrix = new CoordinatesMatrix(buffer);

        // Act & Assert (Full Scan)
        for (ushort i = 0; i < area; i++)
        {
            var expectedX = i % width;
            var expectedY = i / width;

            // Accesso tramite Indexer
            var coord = matrix[i];

            if (coord.X != expectedX || coord.Y != expectedY)
            {
                Fail($"Coordinate Mismatch at Index {i}. Expected: ({expectedX},{expectedY}), Actual: ({coord.X},{coord.Y})");
            }
        }
    }

    [Test]
    public void Get_WhenUnderlyingMemoryChanges_ThenViewUpdatesInstantly()
    {
        var buffer = new Coordinate[1];
        buffer[0] = new Coordinate(0, 0);
        var matrix = new CoordinatesMatrix(buffer);

        // Mutazione diretta della memoria
        buffer[0] = new Coordinate(255, 255);

        That(matrix[0].X, Is.EqualTo(255), "Matrix view is not reflecting memory updates.");
    }
}