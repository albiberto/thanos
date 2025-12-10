using Thanos.Shared;
using Thanos.SourceGen;
using static NUnit.Framework.Assert;

namespace Thanos.Tests.Shared;

[TestFixture]
public class CoordinatesMatrixTests
{
    private static object[][] Dimensions => Support.Dimensions;

    /// <summary>
    ///     Verifies that CoordinatesMatrix correctly reads coordinate values from the underlying memory buffer
    ///     using both Get() method and indexer syntax across different grid dimensions.
    /// </summary>
    [TestCaseSource(nameof(Dimensions))]
    public void Matrix_ShouldRead_Correctly_FromUnderlyingMemory(byte width, byte height, ushort area)
    {
        var buffer = new Coordinate[area];
        for (ushort i = 0; i < area; i++) buffer[i] = new Coordinate((byte)(i % width), (byte)(i / width));

        var matrix = new CoordinatesMatrix(buffer);

        for (ushort i = 0; i < area; i++)
        {
            var expected = buffer[i];

            using (EnterMultipleScope())
            {
                That(matrix.Get(i), Is.EqualTo(expected), $"Get({i}) returned an incorrect value.");
                That(matrix[i], Is.EqualTo(expected), $"Indexer [{i}] returned an incorrect value.");
            }
        }
    }

    /// <summary>
    ///     Verifies that CoordinatesMatrix reflects changes made to the underlying memory buffer,
    ///     ensuring the matrix acts as a view over the buffer rather than a copy.
    /// </summary>
    [Test]
    public void Matrix_ShouldReflect_ChangesInUnderlyingMemory()
    {
        var buffer = new Coordinate[2];
        buffer[0] = new Coordinate(0, 0);
        var matrix = new CoordinatesMatrix(buffer);

        buffer[0] = new Coordinate(99, 99);

        using (EnterMultipleScope())
        {
            That(matrix[0], Is.EqualTo(new Coordinate(99, 99)), "Matrix should reflect changes in underlying buffer.");
        }
    }
}