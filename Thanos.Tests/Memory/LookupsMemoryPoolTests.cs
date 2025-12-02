using Thanos.Memory;
using Thanos.Common;
using Thanos.Shared;
using Thanos.SourceGen;
using static NUnit.Framework.Assert;

namespace Thanos.Tests.Memory;

[TestFixture]
public class LookupsMemoryPoolTests
{
    private static object[][] Dimensions => Support.Dimensions;

    [TestCaseSource(nameof(Dimensions))]
    public void Pool_ShouldInitialize_AndContainCorrectData(byte width, byte height, ushort area)
    {
        // 1. Arrange & Act
        using var pool = GetPoolByWidth(area);

        That(pool.CoordinatesMatrix[0], Is.EqualTo(new Coordinate(0, 0)), "First coordinate should be (0,0)");
        var lastIndex = (ushort)(area - 1);
        That(pool.CoordinatesMatrix[lastIndex], Is.EqualTo(new Coordinate((byte)(width - 1), (byte)(height - 1))), "Last coordinate should match grid dimensions");
        var upNeighbor = pool.NeighborsMatrix.Get(0, Moves.Up);
        That(upNeighbor, Is.EqualTo(width), "Neighbor UP from (0,0) should be index 'width'");

        var downNeighbor = pool.NeighborsMatrix.Get(0, Moves.Down);
        That(NeighborsMatrix.IsValid(downNeighbor), Is.False, "Neighbor DOWN from (0,0) should be Invalid");
    }

    [Test]
    public void Factories_ShouldReturnNewInstances()
    {
        using var pool1 = LookupsMemoryPool.Small;
        using var pool2 = LookupsMemoryPool.Small;

        That(pool1, Is.Not.SameAs(pool2));
        That(pool1.CoordinatesMatrix[0], Is.EqualTo(pool2.CoordinatesMatrix[0]));
    }
    
    [Test]
    public void Dispose_ShouldRunWithoutExceptions()
    {
        var pool = LookupsMemoryPool.Small;
        DoesNotThrow(() => pool.Dispose());
    }

    private static LookupsMemoryPool GetPoolByWidth(ushort area) =>
        area switch
        {
            var w when w == Constants.Small.Area => LookupsMemoryPool.Small,
            var w when w == Constants.Medium.Area => LookupsMemoryPool.Medium,
            var w when w == Constants.Large.Area => LookupsMemoryPool.Large,
            _ => throw new ArgumentException($"Nessun pool configurato per area {area}")
        };
}