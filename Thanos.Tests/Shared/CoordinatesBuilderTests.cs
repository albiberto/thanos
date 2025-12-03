using Thanos.Shared;
using Thanos.SourceGen;
using static NUnit.Framework.Assert;

namespace Thanos.Tests.Shared;

[TestFixture]
public class CoordinatesBuilderTests
{
    private static object[][] Dimensions => Support.Dimensions;

    /// <summary>
    ///     Verifies that CoordinatesBuilder.Populate fills a coordinate buffer using correct row-major order logic,
    ///     where each coordinate maps to (x = index % width, y = index / width) for various grid dimensions.
    /// </summary>
    [TestCaseSource(nameof(Dimensions))]
    public void Populate_ShouldFillCoordinates_WithCorrectRowMajorLogic(byte width, byte height, ushort area)
    {
        var buffer = new Coordinate[area];

        CoordinatesBuilder.Populate(width, height, buffer);

        for (var i = 0; i < area; i++)
        {
            var expectedX = (byte)(i % width);
            var expectedY = (byte)(i / width);
            var expectedCoord = new Coordinate(expectedX, expectedY);

            using (EnterMultipleScope())
            {
                That(buffer[i], Is.EqualTo(expectedCoord), $"Error at index {i}: expected {expectedCoord}, found {buffer[i]}");
            }
        }
    }

    /// <summary>
    ///     Verifies that CoordinatesBuilder.Populate throws an ArgumentException when the buffer size
    ///     does not match the expected dimensions (width * height).
    /// </summary>
    [TestCaseSource(nameof(Dimensions))]
    public void Populate_ShouldThrow_WhenBufferSizeDoesNotMatchDimensions(byte width, byte height, ushort area)
    {
        var wrongBuffer = new Coordinate[area - 1];

        using (EnterMultipleScope())
        {
            Throws<ArgumentException>(() => CoordinatesBuilder.Populate(width, height, wrongBuffer));
        }
    }
}