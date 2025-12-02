using Thanos.Common;
using Thanos.Shared;

namespace Thanos.Tests.Shared;

[TestFixture]
public class NeighborsMatrixTests
{
    [TestCase(7)]
    [TestCase(11)]
    [TestCase(19)]
    public void Should_Populate_Neighbors_Correctly(byte width)
    {
        var area = width * width;
        var memory = new ushort[area * 4];

        NeighborsMatrix.PlacementNew(width, memory);
        
        var grid = new NeighborsMatrix(memory);

        // Assert 1: Angolo in basso a sinistra (0,0) -> Indice 0
        // UP: (0,1) -> Indice 11
        // DOWN: Fuori -> MaxValue
        // LEFT: Fuori -> MaxValue
        // RIGHT: (1,0) -> Indice 1
        Assert.Multiple(() =>
        {
            Assert.That(grid.Get(0, Moves.Up), Is.EqualTo(11), "Bottom-Left UP should be 11");
            Assert.That(grid.Get(0, Moves.Down), Is.EqualTo(ushort.MaxValue), "Bottom-Left DOWN should be invalid");
            Assert.That(grid.Get(0, Moves.Left), Is.EqualTo(ushort.MaxValue), "Bottom-Left LEFT should be invalid");
            Assert.That(grid.Get(0, Moves.Right), Is.EqualTo(1), "Bottom-Left RIGHT should be 1");
        });

        // Assert 2: Angolo in alto a destra (10,10) -> Indice 120
        // UP: Fuori -> MaxValue
        // DOWN: (10,9) -> Indice 109 (120 - 11)
        // LEFT: (9,10) -> Indice 119
        // RIGHT: Fuori -> MaxValue
        Assert.Multiple(() =>
        {
            Assert.That(grid.Get(120, Moves.Up), Is.EqualTo(ushort.MaxValue), "Top-Right UP should be invalid");
            Assert.That(grid.Get(120, Moves.Down), Is.EqualTo(109), "Top-Right DOWN should be 109");
            Assert.That(grid.Get(120, Moves.Left), Is.EqualTo(119), "Top-Right LEFT should be 119");
            Assert.That(grid.Get(120, Moves.Right), Is.EqualTo(ushort.MaxValue), "Top-Right RIGHT should be invalid");
        });

        // Assert 3: Centro (5,5) -> Indice 60
        // UP: (5,6) -> 71 (+11)
        // DOWN: (5,4) -> 49 (-11)
        // LEFT: (4,5) -> 59 (-1)
        // RIGHT: (6,5) -> 61 (+1)
        Assert.Multiple(() =>
        {
            Assert.That(grid.Get(60, Moves.Up), Is.EqualTo(71), "Center UP should be +11");
            Assert.That(grid.Get(60, Moves.Down), Is.EqualTo(49), "Center DOWN should be -11");
            Assert.That(grid.Get(60, Moves.Left), Is.EqualTo(59), "Center LEFT should be -1");
            Assert.That(grid.Get(60, Moves.Right), Is.EqualTo(61), "Center RIGHT should be +1");
        });
    }

    [Test]
    public void Build_ShouldThrowException_WhenMemoryLengthInvalid()
    {
        var memory = new ushort[10]; // Non divisibile per 4
        
        Assert.Throws<ArgumentException>(() => NeighborsMatrix.PlacementNew(11, memory));
    }
}