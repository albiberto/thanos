using Thanos.Common;
using Thanos.Memory;
using static NUnit.Framework.Assert;

namespace Thanos.Tests.Memory;

[TestFixture]
public class LookupsMemoryPoolTests
{
    [Test]
    public void Singleton_WhenAccessedMultipleTimes_ThenReturnsSameInstance()
    {
        var instance1 = LookupsMemoryPool.Medium;
        var instance2 = LookupsMemoryPool.Medium;

        That(instance1, Is.SameAs(instance2), "Singleton violates identity constraint.");
    }

    [Test]
    public void Constructor_WhenInitialized_ThenPopulatesDataCorrectly()
    {
        // Verifica integrazione con Builders e accessibilità memoria
        using var pool = new LookupsMemoryPool(11, 11, 121);

        // Check a campione (Data Integrity)
        // Nota: Qui leggiamo per valore, quindi nessun problema con ref struct
        var coord = pool.CoordinatesMatrix[120]; // (10,10)
        var neighbor = pool.NeighborsMatrix.Get(0, Moves.Up); // 11

        Multiple(() =>
        {
            That(coord.X, Is.EqualTo(10));
            That(coord.Y, Is.EqualTo(10));
            That(neighbor, Is.EqualTo(11));
        });
    }

    [Test]
    public void Dispose_WhenCalled_ThenDoesNotThrow()
    {
        var pool = new LookupsMemoryPool(5, 5, 25);
        DoesNotThrow(() => pool.Dispose());
    }
}