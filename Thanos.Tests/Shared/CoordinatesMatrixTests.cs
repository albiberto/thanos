using Thanos.Shared;
using Thanos.SourceGen;
using Thanos.Tests.SourceGen;
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
            var actualGet = matrix.Get(i);
            var actualIndexer = matrix[i];

            Multiple(() =>
            {
                That(actualGet, Is.EqualTo(expected), $"Get({i}) should return {expected} but was {actualGet}.");
                That(actualIndexer, Is.EqualTo(expected), $"Indexer[{i}] should return {expected} but was {actualIndexer}.");
            });
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
        var expected = new Coordinate(99, 99);
        var actual = matrix[0];

        That(actual, Is.EqualTo(expected), $"Matrix[0] should be {expected} reflecting underlying buffer change but was {actual}.");
    }
}