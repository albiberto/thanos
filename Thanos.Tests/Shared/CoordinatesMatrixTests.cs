using Thanos.Shared;
using Thanos.SourceGen;
using static NUnit.Framework.Assert;

namespace Thanos.Tests.Shared;

[TestFixture]
public class CoordinatesMatrixTests
{
    private static object[][] Dimensions => Support.Dimensions;

    [TestCaseSource(nameof(Dimensions))]
    public void Matrix_ShouldRead_Correctly_FromUnderlyingMemory(byte width, byte height, ushort area)
    {
        // Arrange
        var buffer = new Coordinate[area];
        for (ushort i = 0; i < area; i++) buffer[i] = new Coordinate((byte)(i % width), (byte)(i / width));

        // Act
        var matrix = new CoordinatesMatrix(buffer);

        // Assert
        for (ushort i = 0; i < area; i++)
        {
            var expected = buffer[i];

            using (EnterMultipleScope())
            {
                That(matrix.Get(i), Is.EqualTo(expected), $"Get({i}) returned an incorrect value.");
                That(matrix[i], Is.EqualTo(expected), $"Indexer [{i}] returned an incorrect value.");
            }
        }
    }

    [Test]
    public void Matrix_ShouldReflect_ChangesInUnderlyingMemory()
    {
        // Arrange
        var buffer = new Coordinate[2];
        buffer[0] = new Coordinate(0, 0);
        var matrix = new CoordinatesMatrix(buffer);

        // Act
        buffer[0] = new Coordinate(99, 99);

        // Assert
        That(matrix[0], Is.EqualTo(new Coordinate(99, 99)));
    }
}