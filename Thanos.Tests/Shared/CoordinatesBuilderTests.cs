using Thanos.Shared;
using Thanos.SourceGen;
using static NUnit.Framework.Assert;

namespace Thanos.Tests.Shared;

[TestFixture]
public class CoordinatesBuilderTests
{
    private static object[][] Dimensions => Support.Dimensions;
    
    [TestCaseSource(nameof(Dimensions))]
    public void Populate_ShouldFillCoordinates_WithCorrectRowMajorLogic(byte width, byte height, ushort area)
    {
        // Arrange
        var buffer = new Coordinate[area];

        // Act
        CoordinatesBuilder.Populate(width, height, buffer);

        // Assert
        for (var i = 0; i < area; i++)
        {
            var expectedX = (byte)(i % width);
            var expectedY = (byte)(i / width);
            var expectedCoord = new Coordinate(expectedX, expectedY);

            That(buffer[i], Is.EqualTo(expectedCoord), $"Error at index {i}: expected {expectedCoord}, found {buffer[i]}");
        }
    }

    [TestCaseSource(nameof(Dimensions))]
    public void Populate_ShouldThrow_WhenBufferSizeDoesNotMatchDimensions(byte width, byte height, ushort area)
    {
        // Arrange
        var wrongBuffer = new Coordinate[area - 1];

        // Act & Assert
        Throws<ArgumentException>(() => CoordinatesBuilder.Populate(width, height, wrongBuffer));
    }
}