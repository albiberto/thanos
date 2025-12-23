using Thanos.Common;
using Thanos.Shared;
using static NUnit.Framework.Assert;

namespace Thanos.Tests.Shared;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class NeighborsBuilderTests
{
    private static object[][] Dimensions => Support.Dimensions;

    [TestCaseSource(nameof(Dimensions))]
    public void Populate_WhenGridIsInitialized_ThenMapsTopologyCorrectly(byte width, byte height, ushort area)
    {
        // Arrange
        var buffer = new ushort[area * 4];

        // Act
        NeighborsBuilder.Populate(width, height, buffer);
        var grid = new NeighborsMatrix(buffer);

        // Verifica un punto centrale sicuro (es. 5,5 su 11x11) -> Index 60
        var center = (ushort)(width * height / 2); 
        
        // ESTRAZIONE VALORI (Pre-Assert)
        // Dobbiamo leggere dalla ref struct PRIMA della lambda di Assert.Multiple
        var actualRight = grid.Get(center, Moves.Right);
        var actualLeft = grid.Get(center, Moves.Left);
        var actualUp = grid.Get(center, Moves.Up);
        var actualDown = grid.Get(center, Moves.Down);

        // Assert
        Multiple(() =>
        {
            That(actualRight, Is.EqualTo(center + 1), "Center Right mismatch");
            That(actualLeft, Is.EqualTo(center - 1), "Center Left mismatch");
            That(actualUp, Is.EqualTo(center + width), "Center Up mismatch (+Width)");
            That(actualDown, Is.EqualTo(center - width), "Center Down mismatch (-Width)");
        });
    }

    [TestCaseSource(nameof(Dimensions))]
    public void Populate_WhenCheckingBoundaries_ThenUsesSentinelValue(byte width, byte height, ushort area)
    {
        // Arrange
        var buffer = new ushort[area * 4];
        NeighborsBuilder.Populate(width, height, buffer);
        var grid = new NeighborsMatrix(buffer);

        // ESTRAZIONE VALORI (Pre-Assert)
        // Bordo Sinistro (x=0)
        var leftBoundaryVal = grid.Get(0, Moves.Left);
        
        // Bordo Destro (x=w-1)
        var rightEdgeIndex = (ushort)(width - 1);
        var rightBoundaryVal = grid.Get(rightEdgeIndex, Moves.Right);
        
        // Bordo Inferiore (y=0)
        var bottomBoundaryVal = grid.Get(0, Moves.Down);

        // Assert
        Multiple(() =>
        {
            That(leftBoundaryVal, Is.EqualTo(ushort.MaxValue), "Left boundary failed (should be Invalid)");
            That(rightBoundaryVal, Is.EqualTo(ushort.MaxValue), "Right boundary failed (should be Invalid)");
            That(bottomBoundaryVal, Is.EqualTo(ushort.MaxValue), "Bottom boundary failed (should be Invalid)");
        });
    }

    [TestCaseSource(nameof(Dimensions))]
    public void Populate_WhenBufferSizeMismatch_ThenThrowsArgumentException(byte width, byte height, ushort area)
    {
        // Arrange
        var wrongBuffer = new ushort[area * 4 - 1]; // 1 byte in meno

        // Act & Assert
        // Qui non usiamo grid, quindi la lambda è sicura (NeighborsBuilder è static)
        Throws<ArgumentException>(() => NeighborsBuilder.Populate(width, height, wrongBuffer));
    }
}