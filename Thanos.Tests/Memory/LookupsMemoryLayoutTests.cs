using Thanos.Memory;
using Thanos.SourceGen;
using static NUnit.Framework.Assert;

namespace Thanos.Tests.Memory;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class LookupsMemoryLayoutTests
{
    private static object[][] Dimensions => Support.Dimensions;

    [TestCaseSource(nameof(Dimensions))]
    public void Constructor_WhenStandardDimensions_ThenCalculatesOffsetsWithoutOverlap(byte width, byte height, ushort area)
    {
        // Arrange
        var layout = new LookupsMemoryLayout(area);

        // Act
        var coordsEnd = layout.Coordinates.Offset + layout.Coordinates.Length;
        // RIMOSSO: var neighborsOffset = layout.Coordinates.Next; (Inutilizzata)

        // Assert
        Multiple(() =>
        {
            // 1. Verifica Conteggi
            That(layout.Coordinates.Count<Coordinate>(), Is.EqualTo(area), "Coordinates count mismatch");
            That(layout.Neighbors.Count<ushort>(), Is.EqualTo(area * 4), "Neighbors count mismatch");

            // 2. Verifica Sovrapposizioni e Sequenzialità
            // Il blocco Neighbors deve iniziare DOPO la fine dei Coordinates
            That(layout.Neighbors.Offset, Is.GreaterThanOrEqualTo(coordsEnd), "Memory overlap detected!");
            
            // Verifica che inizi esattamente al prossimo slot allineato
            That(layout.Neighbors.Offset, Is.EqualTo(layout.Coordinates.Next), "Neighbors should start at Coordinates.Next alignment boundary.");
            
            // 3. Verifica Allineamento
            That((long)layout.Neighbors.Offset % Constants.CacheLine, Is.Zero, "Neighbors block must be aligned to CacheLine (64 bytes).");
            That((long)layout.TotalSize % Constants.CacheLine, Is.Zero, "Total size must be aligned to CacheLine.");
        });
    }

    [Test]
    public void Constructor_WhenDataIsSmallerThanCacheLine_ThenAppliesPadding()
    {
        // Arrange
        var layout = new LookupsMemoryLayout(1);

        // Assert
        Multiple(() =>
        {
            That(layout.Coordinates.Length, Is.EqualTo((nuint)2)); // 1 * 2 bytes
            That(layout.Neighbors.Offset, Is.EqualTo((nuint)64), "Padding was not applied. False sharing risk.");
        });
    }

    [Test]
    public void Constructor_WhenDataExceedsCacheLineByOneByte_ThenJumpsToNextLine()
    {
        // Arrange
        var layout = new LookupsMemoryLayout(33);

        // Assert
        That(layout.Neighbors.Offset, Is.EqualTo((nuint)128), "Alignment logic failed for >64 bytes.");
    }
}