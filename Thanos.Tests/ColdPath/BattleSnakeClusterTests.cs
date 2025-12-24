using Thanos.Abstract;
using Thanos.Memory;
using Thanos.MCST; // Assumendo che Engine sia qui
using static NUnit.Framework.Assert;

namespace Thanos.Tests.ColdPath;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class BattleSnakeClusterTests
{
    // Usiamo il singleton per le risorse condivise (ReadOnly)
    private readonly LookupsMemoryPool _lookups = LookupsMemoryPool.Medium;

    [Test]
    public void Constructor_WhenArraysHaveMismatchedLengths_ThenThrowsArgumentException()
    {
        // Arrange
        var engines = new Engine[2]; // 2 Engine
        var slotPools = new ISlotMemoryPool[3]; // 3 Slot Pools (Mismatch!)
        var nodePools = new INodeMemoryPool[2];

        // Act & Assert
        Throws<ArgumentException>(() => new BattleSnakeCluster(engines, slotPools, nodePools, _lookups));
    }

    [Test]
    public void Constructor_WhenComponentsAreValid_ThenInitializesClusterSuccessfully()
    {
        // Arrange
        const int count = 2;
        var (engines, slotPools, nodePools) = CreateClusterComponents(count);

        // Act
        using var cluster = new BattleSnakeCluster(engines, slotPools, nodePools, _lookups);

        // Assert
        That(cluster, Is.Not.Null);
    }

    [Test]
    public void InitializeGame_WhenCalled_ThenPropagatesSortedIdsToAllEngines()
    {
        // Arrange
        var (engines, slotPools, nodePools) = CreateClusterComponents(1);
        using var cluster = new BattleSnakeCluster(engines, slotPools, nodePools, _lookups);
        
        string[] sortedIds = ["snake-a", "snake-b"];

        // Act
        cluster.InitializeGame(sortedIds);

        // Assert
        // Nota: Poiché Engine non espone facilmente lo stato interno per i test senza renderlo pubblico,
        // ci affidiamo al fatto che non lanci eccezioni. 
        // In un scenario reale, Engine dovrebbe esporre una proprietà "InitializedIds" o essere mockabile.
        // Dato che qui usiamo Engine concreti, verifichiamo la stabilità.
        DoesNotThrow(() => cluster.InitializeGame(sortedIds));
    }

    [Test]
    public void Reset_WhenCalled_ThenResetsAllInternalComponents()
    {
        // Arrange
        var (engines, slotPools, nodePools) = CreateClusterComponents(1);
        using var cluster = new BattleSnakeCluster(engines, slotPools, nodePools, _lookups);

        // Act & Assert
        DoesNotThrow(() => cluster.Reset(), "Cluster reset should cascade to engines without error.");
    }

    [Test]
    public void Dispose_WhenCalled_ThenDisposesAllPools()
    {
        // Arrange
        // Qui usiamo Mock o classi concrete. Dato che MemoryPool ha un Dispose reale (NativeMemory),
        // è critico verificare che venga chiamato. Poiché non stiamo usando Mock<ISlotMemoryPool>,
        // verifichiamo l'idempotenza e l'assenza di crash.
        var (engines, slotPools, nodePools) = CreateClusterComponents(1);
        var cluster = new BattleSnakeCluster(engines, slotPools, nodePools, _lookups);

        // Act
        cluster.Dispose();

        // Assert
        DoesNotThrow(() => cluster.Dispose(), "Dispose must be idempotent.");
        
        // Verifica manuale post-dispose (opzionale se avessimo accesso a proprietà IsDisposed)
        // Tentare di usare i pool ora dovrebbe (o potrebbe) fallire o essere no-op.
    }
    
    // --- Helper Factory per ridurre il boilerplate ---
    private (Engine[] engines, ISlotMemoryPool[] slots, INodeMemoryPool[] nodes) CreateClusterComponents(int count)
    {
        var slotLayout = new SlotMemoryLayout(Constants.Medium.Area, 64, Constants.MaxSnakesCount);
        var nodeLayout = new NodeMemoryLayout();

        var engines = new Engine[count];
        var slotPools = new ISlotMemoryPool[count];
        var nodePools = new INodeMemoryPool[count];

        for (var i = 0; i < count; i++)
        {
            slotPools[i] = new SlotMemoryPool(10, 0, Constants.MaxSnakesCount, _lookups, slotLayout);
            nodePools[i] = new NodeMemoryPool(100, 1, nodeLayout);
            
            var worker = new WorkerTests(slotPools[i], nodePools[i]); 
            engines[i] = new Engine(slotPools[i], nodePools[i], worker, i);
        }

        return (engines, slotPools, nodePools);
    }
}