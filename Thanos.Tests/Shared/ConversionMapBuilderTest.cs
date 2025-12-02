using Thanos.Shared;
using Thanos.SourceGen;

namespace Thanos.Tests.Shared;

[TestFixture]
public class ConversionMapBuilderTests
{
    [TestCase(7)]
    [TestCase(11)]
    [TestCase(19)]
    public void Build_ShouldPopulate_Entire11x11Grid_Correctly(byte width)
    {
        // Arrange
        var area = width * width;

        var coordinates = new Coordinate[area];

        // Act
        ConversionMapBuilder.PlacementNew(width, coordinates);

        // Assert
        for (var i = 0; i < area; i++)
        {
            var expectedX = i % width;
            var expectedY = i / width;

            Assert.Multiple(() =>
            {
                Assert.That(coordinates[i].X, Is.EqualTo(expectedX), $"X error at index {i}");
                Assert.That(coordinates[i].Y, Is.EqualTo(expectedY), $"Y error at index {i}");
            });
        }
    }
}