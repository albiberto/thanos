using Thanos.Common;
using Thanos.Shared;
using static NUnit.Framework.Assert;

namespace Thanos.Tests.Shared;

[TestFixture]
public class NeighborsMatrixTests
{
    private static object[][] Dimensions => Support.Dimensions;

    [TestCaseSource(nameof(Dimensions))]
    public void Matrix_ShouldRead_Correctly_FromUnderlyingMemory(byte width, byte height, ushort area)
    {
        // Arrange
        var buffer = new ushort[area * 4];
        for (var i = 0; i < buffer.Length; i++) buffer[i] = (ushort)i;

        byte[] masks = [Moves.Up, Moves.Down, Moves.Left, Moves.Right];

        // Act
        var matrix = new NeighborsMatrix(buffer);

        // Assert
        for (ushort pos = 0; pos < area; pos++)
        for (var moveIndex = 0; moveIndex < 4; moveIndex++)
        {
            var expected = buffer[pos * 4 + moveIndex];
            var moveMask = masks[moveIndex];

            using (EnterMultipleScope())
            {
                That(matrix.GetAt(pos, moveIndex), Is.EqualTo(expected), $"GetAt({pos}, {moveIndex}) returned an incorrect value.");
                That(matrix.Get(pos, moveMask), Is.EqualTo(expected), $"Get({pos}, {moveMask}) returned an incorrect value.");
            }
        }
    }

    [Test]
    public void Matrix_ShouldReflect_ChangesInUnderlyingMemory()
    {
        // Arrange
        var buffer = new ushort[4];
        buffer[0] = 100;
        var matrix = new NeighborsMatrix(buffer);

        // Act
        buffer[0] = 999;

        // Assert
        That(matrix.GetAt(0, 0), Is.EqualTo(999));
    }

    [Test]
    public void IsValid_ShouldReturnCorrectBoolean()
    {
        using (EnterMultipleScope())
        {
            That(NeighborsMatrix.IsValid(ushort.MaxValue), Is.False, "MaxValue should be Invalid");
            That(NeighborsMatrix.IsValid(0), Is.True, "0 should be Valid");
            That(NeighborsMatrix.IsValid(12345), Is.True, "Any other number should be Valid");
        }
    }
}