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
    public unsafe void Layout_ShouldCalculateProperties_ForStandardDimensions(byte width, byte height, ushort area)
    {
        var layout = new LookupsMemoryLayout(area);

        using (EnterMultipleScope())
        {
            That(layout.Coordinates.Length, Is.EqualTo(area), "Coordinates length is incorrect");
            That(layout.Neighbors.Length, Is.EqualTo(area * 4), "Neighbors length is incorrect");
            That(layout.Coordinates.Offset, Is.EqualTo(0), "Coordinates offset must always be 0");
        }

        var coordsEnd = layout.Coordinates.Offset + layout.Coordinates.Length * sizeof(Coordinate);
        var neighborsEnd = layout.Neighbors.Offset + layout.Neighbors.Length * sizeof(ushort);

        using (EnterMultipleScope())
        {
            That((long)layout.Neighbors.Offset % Constants.CacheLine, Is.Zero, "Neighbors offset is not aligned to 64 bytes");
            That(layout.Neighbors.Offset, Is.GreaterThanOrEqualTo(coordsEnd), "Neighbors overlaps with Coordinates");
            That((long)layout.TotalSize % Constants.CacheLine, Is.Zero, "TotalSize is not aligned to 64 bytes");
            That((long)layout.TotalSize, Is.GreaterThanOrEqualTo((long)neighborsEnd), "TotalSize is less than the end of Neighbors block");
        }
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

        using (EnterMultipleScope())
        {
            That(layout.Coordinates.Length, Is.EqualTo(1), "Coordinates length should be 1");
            That(layout.Coordinates.Offset, Is.EqualTo(0), "Coordinates offset should be 0");
            That(layout.Neighbors.Offset, Is.EqualTo(64), "Neighbors offset should be 64");
        }
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

        using (EnterMultipleScope())
        {
            That(layout.Coordinates.Length, Is.EqualTo(32), "Coordinates length should be 32");
            That(layout.Neighbors.Offset, Is.EqualTo(64), "Neighbors offset should be 64");
        }
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

        using (EnterMultipleScope())
        {
            That(layout.Neighbors.Offset, Is.EqualTo(128), "Neighbors offset should be 128");
        }
    }
}