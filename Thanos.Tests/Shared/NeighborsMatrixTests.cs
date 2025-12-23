using Thanos.Common;
using Thanos.Shared;
using Thanos.Tests.SourceGen;
using static NUnit.Framework.Assert;

namespace Thanos.Tests.Shared;

[TestFixture]
public class NeighborsMatrixTests
{
    private static object[][] Dimensions => Support.Dimensions;

    /// <summary>
    ///     Verifies that NeighborsMatrix correctly reads neighbor indices from the underlying memory buffer
    ///     using both GetAt() method (with move index) and Get() method (with move mask) across different grid dimensions.
    /// </summary>
    [TestCaseSource(nameof(Dimensions))]
    public void Matrix_ShouldRead_Correctly_FromUnderlyingMemory(byte width, byte height, ushort area)
    {
        var buffer = new ushort[area * 4];
        for (var i = 0; i < buffer.Length; i++) buffer[i] = (ushort)i;

        byte[] masks = [Moves.Up, Moves.Down, Moves.Left, Moves.Right];

        var matrix = new NeighborsMatrix(buffer);

        for (ushort pos = 0; pos < area; pos++)
        for (var moveIndex = 0; moveIndex < 4; moveIndex++)
        {
            var expected = buffer[pos * 4 + moveIndex];
            var moveMask = masks[moveIndex];
            var actualGetAt = matrix.GetAt(pos, moveIndex);
            var actualGet = matrix.Get(pos, moveMask);

            Multiple(() =>
            {
                That(actualGetAt, Is.EqualTo(expected), $"GetAt({pos}, {moveIndex}) should return {expected} but was {actualGetAt}.");
                That(actualGet, Is.EqualTo(expected), $"Get({pos}, {moveMask}) should return {expected} but was {actualGet}.");
            });
        }
    }

    /// <summary>
    ///     Verifies that NeighborsMatrix reflects changes made to the underlying memory buffer,
    ///     ensuring the matrix acts as a view over the buffer rather than a copy.
    /// </summary>
    [Test]
    public void Matrix_ShouldReflect_ChangesInUnderlyingMemory()
    {
        var buffer = new ushort[4];
        buffer[0] = 100;
        var matrix = new NeighborsMatrix(buffer);

        buffer[0] = 999;
        var expected = (ushort)999;
        var actual = matrix.Get(0, Moves.Up);

        That(actual, Is.EqualTo(expected), $"Matrix.Get(0, Moves.Up) should be {expected} reflecting underlying buffer change but was {actual}.");
    }

    /// <summary>
    ///     Verifies that NeighborsMatrix.IsValid correctly identifies ushort.MaxValue as invalid
    ///     and all other values as valid neighbor indices.
    /// </summary>
    [Test]
    public void IsValid_ShouldReturnCorrectBoolean()
    {
        var actualMaxValue = NeighborsMatrix.IsValid(ushort.MaxValue);
        var actualZero = NeighborsMatrix.IsValid(0);
        var actualOther = NeighborsMatrix.IsValid(12345);

        Multiple(() =>
        {
            That(actualMaxValue, Is.False, $"IsValid(ushort.MaxValue) should be False but was {actualMaxValue}.");
            That(actualZero, Is.True, $"IsValid(0) should be True but was {actualZero}.");
            That(actualOther, Is.True, $"IsValid(12345) should be True but was {actualOther}.");
        });
    }
}