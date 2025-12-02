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

    /// <summary>
    /// Verifies that LookupsMemoryPool initializes correctly and contains accurate coordinate and neighbor data
    /// for standard grid dimensions, validating both CoordinatesMatrix and NeighborsMatrix contents.
    /// </summary>
    [TestCaseSource(nameof(Dimensions))]
    public void Pool_ShouldInitialize_AndContainCorrectData(byte width, byte height, ushort area)
    {
        using var pool = GetPoolByWidth(area);

        var lastIndex = (ushort)(area - 1);
        var upNeighbor = pool.NeighborsMatrix.Get(0, Moves.Up);
        var downNeighbor = pool.NeighborsMatrix.Get(0, Moves.Down);

        using (EnterMultipleScope())
        {
            That(pool.CoordinatesMatrix[0], Is.EqualTo(new Coordinate(0, 0)), "First coordinate should be (0,0)");
            That(pool.CoordinatesMatrix[lastIndex], Is.EqualTo(new Coordinate((byte)(width - 1), (byte)(height - 1))), "Last coordinate should match grid dimensions");
            That(upNeighbor, Is.EqualTo(width), "Neighbor UP from (0,0) should be index 'width'");
            That(NeighborsMatrix.IsValid(downNeighbor), Is.False, "Neighbor DOWN from (0,0) should be Invalid");
        }
    }

    /// <summary>
    /// Verifies that LookupsMemoryPool factory methods return new distinct instances for each invocation,
    /// while containing equivalent data.
    /// </summary>
    [Test]
    public void Factories_ShouldReturnNewInstances()
    {
        using var pool1 = LookupsMemoryPool.Small;
        using var pool2 = LookupsMemoryPool.Small;

        using (EnterMultipleScope())
        {
            That(pool1, Is.Not.SameAs(pool2), "Factory should return new instances");
            That(pool1.CoordinatesMatrix[0], Is.EqualTo(pool2.CoordinatesMatrix[0]), "Data should be equivalent");
        }
    }
    
    /// <summary>
    /// Verifies that LookupsMemoryPool.Dispose executes successfully without throwing exceptions,
    /// ensuring proper resource cleanup.
    /// </summary>
    [Test]
    public void Dispose_ShouldRunWithoutExceptions()
    {
        var pool = LookupsMemoryPool.Small;
        
        using (EnterMultipleScope())
        {
            DoesNotThrow(() => pool.Dispose(), "Dispose should not throw exceptions");
        }
    }

    private static LookupsMemoryPool GetPoolByWidth(ushort area) =>
        area switch
        {
            var w when w == Constants.Small.Area => LookupsMemoryPool.Small,
            var w when w == Constants.Medium.Area => LookupsMemoryPool.Medium,
            var w when w == Constants.Large.Area => LookupsMemoryPool.Large,
            _ => throw new ArgumentException($"No pool configured for area {area}")
        };
}