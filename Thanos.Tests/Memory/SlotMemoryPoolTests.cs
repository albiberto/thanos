using NUnit.Framework;
using Thanos.Memory;
using Thanos.Common;
using static NUnit.Framework.Assert;

namespace Thanos.Tests.Memory;

[TestFixture]
public class SlotMemoryPoolTests
{
    private SlotMemoryPool? _pool;
    private LookupsMemoryPool _lookups;
    private const uint MaxSlots = 10;

    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        // Usiamo un pool di lookup reale per i test
        _lookups = LookupsMemoryPool.Medium;
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _lookups.Dispose();
    }

    [TearDown]
    public void TearDown()
    {
        _pool?.Dispose();
    }

    [Test]
    public void Constructor_Should_Initialize_Capacity_And_Count()
    {
        _pool = new SlotMemoryPool(MaxSlots, _lookups, SlotMemoryLayout.Medium);

        using (EnterMultipleScope())
        {
            That(_pool.Capacity, Is.EqualTo((int)MaxSlots));
            That(_pool.Index, Is.EqualTo(0), "SlotPool starts at 0 (unlike NodePool)");
        }
    }

    [Test]
    public void Allocate_Should_Return_SequentialIndices()
    {
        _pool = new SlotMemoryPool(MaxSlots, _lookups, SlotMemoryLayout.Medium);

        var idx1 = _pool.Allocate();
        var idx2 = _pool.Allocate();

        using (EnterMultipleScope())
        {
            That(idx1, Is.EqualTo(0));
            That(idx2, Is.EqualTo(1));
            That(_pool.Index, Is.EqualTo(2));
        }
    }

    [Test]
    public void Configure_Should_ResetCount_And_SetSnakeCount()
    {
        _pool = new SlotMemoryPool(MaxSlots, _lookups, SlotMemoryLayout.Medium);
        _pool.Allocate();
        _pool.Allocate();

        // Act: Configura per una nuova partita con 4 serpenti
        _pool.Configure(4);

        using (EnterMultipleScope())
        {
            That(_pool.Index, Is.EqualTo(0), "Configure should reset the allocator");
            
            // Verifichiamo che l'allocazione riparta da 0
            That(_pool.Allocate(), Is.EqualTo(0));
        }
    }

    [Test]
    public void GetArena_Should_Return_Arena_With_CorrectSnakeCount()
    {
        _pool = new SlotMemoryPool(MaxSlots, _lookups, SlotMemoryLayout.Medium);
        
        // Configuriamo per 3 serpenti
        int expectedSnakeCount = 3;
        _pool.Configure(expectedSnakeCount);
        
        int slotIndex = _pool.Allocate();
        
        // Act
        var arena = _pool.GetArena(slotIndex);

        // Assert
        using (EnterMultipleScope())
        {
            That(arena.Snakes.Count, Is.EqualTo(expectedSnakeCount));
            // Verifica che il riferimento ai neighbors sia corretto
            That(arena.NeighborsMatrix.Width, Is.EqualTo(_lookups.NeighborsMatrix.Width));
        }
    }

    [Test]
    public void Data_Should_Persist_In_Slot()
    {
        _pool = new SlotMemoryPool(MaxSlots, _lookups, SlotMemoryLayout.Medium);
        _pool.Configure(2); // 2 Serpenti
        
        int slotIndex = _pool.Allocate();
        
        // Scrittura Dati (tramite Arena View)
        var arena = _pool.GetArena(slotIndex);
        
        // Modifichiamo la vita del serpente 0
        arena.Snakes[0].Life.Health = 99;
        // Modifichiamo la lunghezza del serpente 1
        arena.Snakes[1].Body.Enqueue(123);

        // Rilettura (nuova view sullo stesso slot)
        var arenaRead = _pool.GetArena(slotIndex);

        using (EnterMultipleScope())
        {
            That(arenaRead.Snakes[0].Life.Health, Is.EqualTo(99));
            That(arenaRead.Snakes[1].Body.Head, Is.EqualTo(123));
        }
    }

    [Test]
    public void Slots_Should_Not_Overlap()
    {
        _pool = new SlotMemoryPool(MaxSlots, _lookups, SlotMemoryLayout.Medium);
        _pool.Configure(1);
        
        int idx1 = _pool.Allocate();
        int idx2 = _pool.Allocate();

        var arena1 = _pool.GetArena(idx1);
        var arena2 = _pool.GetArena(idx2);

        // Scriviamo su Arena 1
        arena1.Snakes[0].Life.Health = 10;
        
        // Scriviamo su Arena 2
        arena2.Snakes[0].Life.Health = 20;

        using (EnterMultipleScope())
        {
            That(arena1.Snakes[0].Life.Health, Is.EqualTo(10), "Writing to Slot 2 should not affect Slot 1");
            That(arena2.Snakes[0].Life.Health, Is.EqualTo(20));
        }
    }
}