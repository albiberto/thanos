using Thanos.Common;
using Thanos.Shared;
using static NUnit.Framework.Assert;

namespace Thanos.Tests.Shared;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class NeighborsMatrixTests
{
    private static object[][] Dimensions => Support.Dimensions;

    [TestCaseSource(nameof(Dimensions))]
    public void Get_WhenIteratingEveryCell_ThenTopologyIsPerfect(byte width, byte height, ushort area)
    {
        // Arrange
        var buffer = new ushort[area * 4];
        NeighborsBuilder.Populate(width, height, buffer); // Usiamo il builder per popolare
        var matrix = new NeighborsMatrix(buffer);

        // Act & Assert (Full Scan)
        for (ushort i = 0; i < area; i++)
        {
            var x = i % width;
            var y = i / width;

            // Calcoliamo l'atteso (Oracle Logic)
            var expectedUp = y < height - 1 ? (ushort)(i + width) : ushort.MaxValue;
            var expectedDown = y > 0 ? (ushort)(i - width) : ushort.MaxValue;
            var expectedLeft = x > 0 ? (ushort)(i - 1) : ushort.MaxValue;
            var expectedRight = x < width - 1 ? (ushort)(i + 1) : ushort.MaxValue;

            // Eseguiamo le query sulla matrice
            var actualUp = matrix.Get(i, Moves.Up);
            var actualDown = matrix.Get(i, Moves.Down);
            var actualLeft = matrix.Get(i, Moves.Left);
            var actualRight = matrix.Get(i, Moves.Right);

            // Verifica puntuale
            if (actualUp != expectedUp)
                Fail($"Topology Error at ({x},{y}) [Index {i}] Moving UP. Expected: {expectedUp}, Actual: {actualUp}");
            
            if (actualDown != expectedDown)
                Fail($"Topology Error at ({x},{y}) [Index {i}] Moving DOWN. Expected: {expectedDown}, Actual: {actualDown}");
            
            if (actualLeft != expectedLeft)
                Fail($"Topology Error at ({x},{y}) [Index {i}] Moving LEFT. Expected: {expectedLeft}, Actual: {actualLeft}");
            
            if (actualRight != expectedRight)
                Fail($"Topology Error at ({x},{y}) [Index {i}] Moving RIGHT. Expected: {expectedRight}, Actual: {actualRight}");
        }
    }

    [Test]
    public void IsValid_Checks_Against_MaxValue()
    {
        Multiple(() =>
        {
            That(NeighborsMatrix.IsValid(ushort.MaxValue), Is.False, "MaxValue must be Invalid");
            That(NeighborsMatrix.IsValid(0), Is.True, "0 must be Valid");
            That(NeighborsMatrix.IsValid(123), Is.True, "123 must be Valid");
        });
    }

    [Test]
    public void Get_Reflects_MemoryChanges()
    {
        var buffer = new ushort[4]; // 1 cella * 4 mosse
        buffer[0] = 10; // Up
        var matrix = new NeighborsMatrix(buffer);

        buffer[0] = 99;

        That(matrix.Get(0, Moves.Up), Is.EqualTo(99));
    }
}