using Thanos.Memory;
using Thanos.SourceGen;
using static NUnit.Framework.Assert;

namespace Thanos.Tests.Memory;

[TestFixture]
public class LookupsMemoryLayoutTests
{
    private static object[][] Dimensions => Support.Dimensions;

    /// <summary>
    ///     Verifies that LookupsMemoryLayout correctly calculates memory layout properties (offsets, lengths, alignment)
    ///     for standard grid dimensions, ensuring proper cache-line alignment and no memory overlaps.
    /// </summary>
    [TestCaseSource(nameof(Dimensions))]
    public void Layout_ShouldCalculateProperties_ForStandardDimensions(byte width, byte height, ushort area)
    {
        var layout = new LookupsMemoryLayout(area);

        var expectedCoordinatesCount = area;
        var expectedNeighborsCount = area * 4;
        const UIntPtr expectedCoordinatesOffset = 0;

        var actualCoordinatesCount = layout.Coordinates.Count<Coordinate>();
        var actualNeighborsCount = layout.Neighbors.Count<ushort>();
        var actualCoordinatesOffset = layout.Coordinates.Offset;

        Multiple(() =>
        {
            That(actualCoordinatesCount, Is.EqualTo(expectedCoordinatesCount), $"Coordinates.Count<Coordinate>() should be {expectedCoordinatesCount} but was {actualCoordinatesCount}.");
            That(actualNeighborsCount, Is.EqualTo(expectedNeighborsCount), $"Neighbors.Count<ushort>() should be {expectedNeighborsCount} but was {actualNeighborsCount}.");
            That(actualCoordinatesOffset, Is.EqualTo(expectedCoordinatesOffset), $"Coordinates.Offset should be {expectedCoordinatesOffset} but was {actualCoordinatesOffset}.");
        });

        var coordsEnd = layout.Coordinates.Offset + layout.Coordinates.Length;
        var neighborsEnd = layout.Neighbors.Offset + layout.Neighbors.Length;

        var neighborsOffsetRemainder = (long)layout.Neighbors.Offset % Constants.CacheLine;
        var totalSizeRemainder = (long)layout.TotalSize % Constants.CacheLine;

        Multiple(() =>
        {
            That(neighborsOffsetRemainder, Is.Zero, $"Neighbors.Offset should be aligned to {Constants.CacheLine} bytes but remainder was {neighborsOffsetRemainder}.");
            That(layout.Neighbors.Offset, Is.GreaterThanOrEqualTo(coordsEnd), $"Neighbors.Offset ({layout.Neighbors.Offset}) should be >= coordsEnd ({coordsEnd}) to avoid overlap.");
            That(totalSizeRemainder, Is.Zero, $"TotalSize should be aligned to {Constants.CacheLine} bytes but remainder was {totalSizeRemainder}.");
            That((long)layout.TotalSize, Is.GreaterThanOrEqualTo((long)neighborsEnd), $"TotalSize ({layout.TotalSize}) should be >= neighborsEnd ({neighborsEnd}).");
        });
    }

    /// <summary>
    ///     Verifies that LookupsMemoryLayout applies proper padding when Coordinates do not fill a complete cache line,
    ///     ensuring the Neighbors section starts at the next aligned cache line boundary (64 bytes).
    ///     Scenario: Area = 1, Coordinates = 2 bytes, expected Neighbors offset = 64.
    /// </summary>
    [Test]
    public void Layout_ShouldApplyPadding_WhenCoordinatesDoNotFillCacheLine()
    {
        var layout = new LookupsMemoryLayout(1);

        var actualCoordinatesCount = layout.Coordinates.Count<Coordinate>();
        var actualCoordinatesOffset = layout.Coordinates.Offset;
        var actualNeighborsOffset = layout.Neighbors.Offset;

        Multiple(() =>
        {
            That(actualCoordinatesCount, Is.EqualTo(1), $"Coordinates.Count<Coordinate>() should be 1 but was {actualCoordinatesCount}.");
            That(actualCoordinatesOffset, Is.EqualTo((nuint)0), $"Coordinates.Offset should be 0 but was {actualCoordinatesOffset}.");
            That(actualNeighborsOffset, Is.EqualTo((nuint)64), $"Neighbors.Offset should be 64 but was {actualNeighborsOffset}.");
        });
    }

    /// <summary>
    ///     Verifies that LookupsMemoryLayout does not apply extra padding when Coordinates exactly fill cache lines,
    ///     allowing Neighbors to start immediately at the next aligned boundary.
    ///     Scenario: Area = 32, Coordinates = 64 bytes, expected Neighbors offset = 64.
    /// </summary>
    [Test]
    public void Layout_ShouldNotApplyPadding_WhenCoordinatesFillExactlyCacheLines()
    {
        var layout = new LookupsMemoryLayout(32);

        var actualCoordinatesCount = layout.Coordinates.Count<Coordinate>();
        var actualNeighborsOffset = layout.Neighbors.Offset;

        Multiple(() =>
        {
            That(actualCoordinatesCount, Is.EqualTo(32), $"Coordinates.Count<Coordinate>() should be 32 but was {actualCoordinatesCount}.");
            That(actualNeighborsOffset, Is.EqualTo((nuint)64), $"Neighbors.Offset should be 64 but was {actualNeighborsOffset}.");
        });
    }

    /// <summary>
    ///     Verifies that LookupsMemoryLayout correctly jumps to the next cache line boundary when Coordinates
    ///     exceed a cache line by any amount, ensuring proper alignment.
    ///     Scenario: Area = 33, Coordinates = 66 bytes, expected Neighbors offset = 128.
    /// </summary>
    [Test]
    public void Layout_ShouldJumpToNextLine_WhenCoordinatesExceedLineByOneByte()
    {
        var layout = new LookupsMemoryLayout(33);

        var actualNeighborsOffset = layout.Neighbors.Offset;

        That(actualNeighborsOffset, Is.EqualTo((nuint)128), $"Neighbors.Offset should be 128 but was {actualNeighborsOffset}.");
    }
}