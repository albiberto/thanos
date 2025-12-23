using Thanos.Shared;
using Thanos.SourceGen;
using static NUnit.Framework.Assert;

namespace Thanos.Tests.Shared;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class CoordinatesBuilderTests
{
    private static object[][] Dimensions => Support.Dimensions;

    [TestCaseSource(nameof(Dimensions))]
    public void Populate_WhenGridIsInitialized_ThenFillsRowMajorCoordinates(byte width, byte height, ushort area)
    {
        // Arrange
        var buffer = new Coordinate[area];

        // Act
        CoordinatesBuilder.Populate(width, height, buffer);

        // Assert
        Multiple(() =>
        {
            // Verifica Row-Major: Indice cresce con X, poi salta riga Y
            // Index 0 -> (0,0)
            That(buffer[0].X, Is.EqualTo(0));
            That(buffer[0].Y, Is.EqualTo(0));

            // Index 1 -> (1,0)
            That(buffer[1].X, Is.EqualTo(1));
            That(buffer[1].Y, Is.EqualTo(0));

            // Index Width -> (0,1)  (Inizio seconda riga)
            That(buffer[width].X, Is.EqualTo(0));
            That(buffer[width].Y, Is.EqualTo(1));

            // Index Last -> (w-1, h-1)
            var last = buffer[area - 1];
            That(last.X, Is.EqualTo(width - 1));
            That(last.Y, Is.EqualTo(height - 1));
        });
    }

    [TestCaseSource(nameof(Dimensions))]
    public void Populate_WhenBufferSizeMismatch_ThenThrowsArgumentException(byte width, byte height, ushort area)
    {
        // Arrange
        var wrongBuffer = new Coordinate[area + 1]; // Troppo grande

        // Act & Assert
        Throws<ArgumentException>(() => CoordinatesBuilder.Populate(width, height, wrongBuffer));
    }
}