using Thanos.Shared;
using Thanos.SourceGen;
using Thanos.Tests.SourceGen;
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
            var actualCoord = buffer[i];

            That(actualCoord, Is.EqualTo(expectedCoord), $"Coordinate at index {i} should be {expectedCoord} but was {actualCoord}.");
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
        Throws<ArgumentException>(() => CoordinatesBuilder.Populate(width, height, wrongBuffer), $"Buffer size mismatch should throw ArgumentException for width={width}, height={height}, area={area}.");
    }
}