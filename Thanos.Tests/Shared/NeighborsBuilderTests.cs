using Thanos.Common;
using Thanos.Shared;
using Thanos.Tests.SourceGen;
using static NUnit.Framework.Assert;

namespace Thanos.Tests.Shared;

[TestFixture]
public class NeighborsBuilderTests
{
    private static object[][] Dimensions => Support.Dimensions;

    /// <summary>
    ///     Verifies that NeighborsBuilder.Populate correctly fills the neighbors matrix with proper indices
    ///     for each direction (Up, Down, Left, Right), respecting grid boundaries by using ushort.MaxValue
    ///     for out-of-bounds neighbors across various grid dimensions.
    /// </summary>
    [TestCaseSource(nameof(Dimensions))]
    public void Populate_ShouldAlignMemory_With_CartesianCoordinates(byte width, byte height, ushort area)
    {
        var buffer = new ushort[area * 4];
        NeighborsBuilder.Populate(width, height, buffer);

        var grid = new NeighborsMatrix(buffer);

        for (ushort i = 0; i < area; i++)
        {
            var x = i % width;
            var y = i / width;

            var actualUp = grid.Get(i, Moves.Up);
            var actualDown = grid.Get(i, Moves.Down);
            var actualLeft = grid.Get(i, Moves.Left);
            var actualRight = grid.Get(i, Moves.Right);

            var expectedUp = y >= height - 1 ? ushort.MaxValue : (ushort)(i + width);
            var expectedDown = y == 0 ? ushort.MaxValue : (ushort)(i - width);
            var expectedLeft = x == 0 ? ushort.MaxValue : (ushort)(i - 1);
            var expectedRight = x == width - 1 ? ushort.MaxValue : (ushort)(i + 1);

            Multiple(() =>
            {
                That(actualUp, Is.EqualTo(expectedUp), $"Neighbor Up at index {i} ({x},{y}) should be {expectedUp} but was {actualUp}.");
                That(actualDown, Is.EqualTo(expectedDown), $"Neighbor Down at index {i} ({x},{y}) should be {expectedDown} but was {actualDown}.");
                That(actualLeft, Is.EqualTo(expectedLeft), $"Neighbor Left at index {i} ({x},{y}) should be {expectedLeft} but was {actualLeft}.");
                That(actualRight, Is.EqualTo(expectedRight), $"Neighbor Right at index {i} ({x},{y}) should be {expectedRight} but was {actualRight}.");
            });
        }
    }

    /// <summary>
    ///     Verifies that NeighborsBuilder.Populate throws an ArgumentException when the buffer size
    ///     does not match the expected dimensions (width * height * 4).
    /// </summary>
    [TestCaseSource(nameof(Dimensions))]
    public void Populate_ShouldThrow_WhenBufferSizeDoesNotMatchDimensions(byte width, byte height, ushort area)
    {
        var expectedLength = area * 4;
        var wrongBuffer = new ushort[expectedLength - 1];

        Throws<ArgumentException>(() => NeighborsBuilder.Populate(width, height, wrongBuffer), $"Buffer size mismatch should throw ArgumentException for width={width}, height={height}, expected length={expectedLength}.");
    }
}