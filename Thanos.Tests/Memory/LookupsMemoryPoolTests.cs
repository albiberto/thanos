using Thanos.Common;
using Thanos.Memory;
using Thanos.Shared;
using Thanos.SourceGen;
using static NUnit.Framework.Assert;

namespace Thanos.Tests.Memory;

[TestFixture]
public class LookupsMemoryPoolTests
{
    private static object[][] Dimensions => Support.Dimensions;

    /// <summary>
    ///     Verifies that LookupsMemoryPool initializes correctly and contains accurate coordinate and neighbor data
    ///     for standard grid dimensions, validating both CoordinatesMatrix and NeighborsMatrix contents.
    /// </summary>
    [TestCaseSource(nameof(Dimensions))]
    public void Pool_ShouldInitialize_AndContainCorrectData(byte width, byte height, ushort area)
    {
        using var pool = GetPoolByWidth(area);

        var lastIndex = (ushort)(area - 1);
        var expectedFirstCoord = new Coordinate(0, 0);
        var expectedLastCoord = new Coordinate((byte)(width - 1), (byte)(height - 1));

        var actualFirstCoord = pool.CoordinatesMatrix[0];
        var actualLastCoord = pool.CoordinatesMatrix[lastIndex];
        var actualUpNeighbor = pool.NeighborsMatrix.Get(0, Moves.Up);
        var actualDownNeighbor = pool.NeighborsMatrix.Get(0, Moves.Down);
        var actualDownNeighborIsValid = NeighborsMatrix.IsValid(actualDownNeighbor);

        Multiple(() =>
        {
            That(actualFirstCoord, Is.EqualTo(expectedFirstCoord),
                $"CoordinatesMatrix[0] should be {expectedFirstCoord} but was {actualFirstCoord}.");
            That(actualLastCoord, Is.EqualTo(expectedLastCoord),
                $"CoordinatesMatrix[{lastIndex}] should be {expectedLastCoord} but was {actualLastCoord}.");
            That(actualUpNeighbor, Is.EqualTo(width),
                $"NeighborsMatrix.Get(0, Moves.Up) should be {width} but was {actualUpNeighbor}.");
            That(actualDownNeighborIsValid, Is.False,
                $"NeighborsMatrix.IsValid(Get(0, Moves.Down)) should be False but was {actualDownNeighborIsValid}.");
        });
    }

    /// <summary>
    ///     Verifies that LookupsMemoryPool factory methods return new distinct instances for each invocation,
    ///     while containing equivalent data.
    /// </summary>
    [Test]
    public void Factories_ShouldReturnNewInstances()
    {
        using var pool1 = LookupsMemoryPool.Medium;
        using var pool2 = LookupsMemoryPool.Medium;

        var expectedCoord = pool2.CoordinatesMatrix[0];
        var actualCoord = pool1.CoordinatesMatrix[0];

        Multiple(() =>
        {
            That(pool1, Is.Not.SameAs(pool2),
                $"Factory should return new instances but returned same reference.");
            That(actualCoord.X, Is.EqualTo(expectedCoord.X),
                $"CoordinatesMatrix[0].X should be {expectedCoord.X} but was {actualCoord.X}.");
            That(actualCoord.Y, Is.EqualTo(expectedCoord.Y),
                $"CoordinatesMatrix[0].Y should be {expectedCoord.Y} but was {actualCoord.Y}.");
        });
    }

    /// <summary>
    ///     Verifies that LookupsMemoryPool.Dispose executes successfully without throwing exceptions,
    ///     ensuring proper resource cleanup.
    /// </summary>
    [Test]
    public void Dispose_ShouldRunWithoutExceptions()
    {
        var pool = LookupsMemoryPool.Medium;

        DoesNotThrow(() => pool.Dispose(),
            "Dispose should not throw exceptions.");
    }

    private static LookupsMemoryPool GetPoolByWidth(ushort area) =>
        area switch
        {
            var w when w == Constants.Medium.Area => LookupsMemoryPool.Medium,
            _ => throw new ArgumentException($"No pool configured for area {area}")
        };
}