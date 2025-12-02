using Thanos.Memory;
using Thanos.SourceGen;

namespace Thanos.Tests.Memory;

[TestFixture]
public class LookupsMemoryLayoutTests
{
    private static object[][] Dimensions => Support.Dimensions;
    
    [TestCaseSource(nameof(Dimensions))]
    public unsafe void Layout_ShouldCalculateProperties_ForStandardDimensions(byte width, byte height, ushort area)
    {
        // Act
        var layout = new LookupsMemoryLayout(area);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(layout.Coordinates.Length, Is.EqualTo(area), "Coordinates Length errata");
            Assert.That(layout.Neighbors.Length, Is.EqualTo(area * 4), "Neighbors Length errata");
            Assert.That(layout.Coordinates.Offset, Is.EqualTo(0), "Coordinates Offset deve essere sempre 0");
        }
    
        var coordsEnd = layout.Coordinates.Offset + (layout.Coordinates.Length * sizeof(Coordinate));
        var neighborsEnd = layout.Neighbors.Offset + (layout.Neighbors.Length * sizeof(ushort));

        using (Assert.EnterMultipleScope())
        {
            Assert.That((long)layout.Neighbors.Offset % Constants.CacheLine, Is.Zero, "Neighbors Offset non è allineato a 64 byte");
            Assert.That(layout.Neighbors.Offset, Is.GreaterThanOrEqualTo(coordsEnd), "Neighbors si sovrappone a Coordinates");
            Assert.That((long)layout.TotalSize % Constants.CacheLine, Is.Zero, "TotalSize non è allineata a 64 byte");
            Assert.That((long)layout.TotalSize, Is.GreaterThanOrEqualTo((long)neighborsEnd),"TotalSize calcolata è inferiore alla fine del blocco Neighbors");
        }
    }

    [Test]
    public void Layout_ShouldApplyPadding_WhenCoordinatesDoNotFillCacheLine()
    {
        // Scenario: Area piccolissima (1 elemento)
        // Coords: 2 byte. 
        // Padding necessario: 62 byte.
        // Neighbors Offset atteso: 64.
        
        // Act
        var layout = new LookupsMemoryLayout(1);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(layout.Coordinates.Length, Is.EqualTo(1));
            Assert.That(layout.Coordinates.Offset, Is.EqualTo(0));
            Assert.That(layout.Neighbors.Offset, Is.EqualTo(64));
        }
    }

    [Test]
    public void Layout_ShouldNotApplyPadding_WhenCoordinatesFillExactlyCacheLines()
    {
        // Scenario: Area = 32.
        // Coords: 32 * 2 byte = 64 byte.
        // Poiché 64 è multiplo di 64, Neighbors dovrebbe iniziare subito a 64 senza buchi extra.
        
        // Act
        var layout = new LookupsMemoryLayout(32);

        using (Assert.EnterMultipleScope())
        {
            // Assert
            Assert.That(layout.Coordinates.Length, Is.EqualTo(32));
            Assert.That(layout.Neighbors.Offset, Is.EqualTo(64));
        }
    }

    [Test]
    public void Layout_ShouldJumpToNextLine_WhenCoordinatesExceedLineByOneByte()
    {
        // Scenario: Area = 33.
        // Coords: 33 * 2 byte = 66 byte.
        // Supera 64 di 2 byte.
        // Neighbors Offset atteso: 128 (64 * 2).
        
        // Act
        var layout = new LookupsMemoryLayout(33);

        // Assert
        Assert.That(layout.Neighbors.Offset, Is.EqualTo(128));
    }
}