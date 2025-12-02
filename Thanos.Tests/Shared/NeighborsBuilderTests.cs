using Thanos.Common;
using Thanos.Shared;
using static NUnit.Framework.Assert;

namespace Thanos.Tests.Shared;

[TestFixture]
public class NeighborsBuilderTests
{
    private static object[][] Dimensions => Support.Dimensions;

    /// <summary>
    /// Verifies that NeighborsBuilder.Populate correctly fills the neighbors matrix with proper indices
    /// for each direction (Up, Down, Left, Right), respecting grid boundaries by using ushort.MaxValue
    /// for out-of-bounds neighbors across various grid dimensions.
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

            var up = grid.Get(i, Moves.Up);
            var down = grid.Get(i, Moves.Down);
            var left = grid.Get(i, Moves.Left);
            var right = grid.Get(i, Moves.Right);

            var expectedUp = y >= height - 1
                ? ushort.MaxValue
                : (ushort)(i + width);

            var expectedDown = y == 0
                ? ushort.MaxValue
                : (ushort)(i - width);

            var expectedLeft = x == 0
                ? ushort.MaxValue
                : (ushort)(i - 1);

            var expectedRight = x == width - 1
                ? ushort.MaxValue
                : (ushort)(i + 1);

            using (EnterMultipleScope())
            {
                That(up, Is.EqualTo(expectedUp), $"Error at {i} ({x},{y}) doing UP");
                That(down, Is.EqualTo(expectedDown), $"Error at {i} ({x},{y}) doing DOWN");
                That(left, Is.EqualTo(expectedLeft), $"Error at {i} ({x},{y}) doing LEFT");
                That(right, Is.EqualTo(expectedRight), $"Error at {i} ({x},{y}) doing RIGHT");
            }
        }
    }

    /// <summary>
    /// Verifies that NeighborsBuilder.Populate throws an ArgumentException when the buffer size
    /// does not match the expected dimensions (width * height * 4).
    /// </summary>
    [TestCaseSource(nameof(Dimensions))]
    public void Populate_ShouldThrow_WhenBufferSizeDoesNotMatchDimensions(byte width, byte height, ushort area)
    {
        var expectedLength = area * 4;
        var wrongBuffer = new ushort[expectedLength - 1];

        using (EnterMultipleScope())
        {
            Throws<ArgumentException>(() => NeighborsBuilder.Populate(width, height, wrongBuffer));
        }
    }
}