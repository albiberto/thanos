using Thanos.Common;
using Thanos.Memory;
using Thanos.Shared;
using static NUnit.Framework.Assert;

namespace Thanos.Tests.Memory;

[TestFixture]
public class LookupsMemoryPoolTests
{
    [Test]
    public void Medium_Pool_Should_Contain_Correct_Grid_Logic()
    {
        var pool = LookupsMemoryPool.Medium;
        var width = Constants.Medium.Width;

        // Test Coordinates Mapping
        var coord0 = pool.CoordinatesMatrix[0];
        That(coord0.X == 0 && coord0.Y == 0, Is.True);

        var coordLast = pool.CoordinatesMatrix[(ushort)(Constants.Medium.Area - 1)];
        That(coordLast.X == 10 && coordLast.Y == 10, Is.True);

        // Test Neighbors Logic
        var upOfZero = pool.NeighborsMatrix.Get(0, Moves.Up); // 0 (0,0) -> Up -> (0,1) -> index 11 (width)
        That(upOfZero, Is.EqualTo(width));

        var downOfZero = pool.NeighborsMatrix.Get(0, Moves.Down); // Fuori mappa
        That(NeighborsMatrix.IsValid(downOfZero), Is.False);
    }

    [Test]
    public void Singleton_Should_Be_Stable()
    {
        var p1 = LookupsMemoryPool.Medium;
        var p2 = LookupsMemoryPool.Medium;
        
        That(p1, Is.SameAs(p2));
    }
}