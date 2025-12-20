using Thanos.Memory;
using static NUnit.Framework.Assert;

namespace Thanos.Tests.Memory;

[TestFixture]
public class SlotMemoryPoolTests
{
    private SlotMemoryPool? _pool;
    private LookupsMemoryPool _lookups;
    private const uint MaxSlots = 10;
    private const byte SnakesCount = 4;
    private const ushort Area = 121; 
    private const ushort QueueCapacity = 128; // Potenza di 2 per ottimizzazione maschera

    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        _lookups = LookupsMemoryPool.Medium;
    }

    [TearDown]
    public void TearDown()
    {
        _pool?.Dispose();
        _pool = null;
    }

    [Test]
    public void Constructor_Should_Initialize_Correctly()
    {
        var layout = new SlotMemoryLayout(Area, QueueCapacity, SnakesCount);
        _pool = new SlotMemoryPool(MaxSlots, 0, SnakesCount, _lookups, layout);

        That(_pool.Capacity, Is.EqualTo(MaxSlots));
        That(_pool.Index, Is.EqualTo(0));
    }

    [Test]
    public void GetArena_Should_Return_Arena_With_Functional_System()
    {
        var layout = new SlotMemoryLayout(Area, QueueCapacity, SnakesCount);
        _pool = new SlotMemoryPool(MaxSlots, 0, SnakesCount, _lookups, layout);

        int slotIndex = _pool.Allocate();
        var arena = _pool.GetArena(slotIndex);
        
        // Inizializza code e stato
        arena.System.Initialize();

        // Verifica accesso ai serpenti
        That(arena.System.Count, Is.EqualTo(SnakesCount));
        
        // Test scritture su memoria nativa tramite astrazione Arena
        arena.System[0].Kill(); 
        That(arena.System[0].IsDead, Is.True);
    }

    [Test]
    public void Slots_Should_Not_Overlap_Memory()
    {
        var layout = new SlotMemoryLayout(Area, QueueCapacity, SnakesCount);
        _pool = new SlotMemoryPool(MaxSlots, 0, SnakesCount, _lookups, layout);

        int idx1 = _pool.Allocate();
        int idx2 = _pool.Allocate();

        var arena1 = _pool.GetArena(idx1);
        var arena2 = _pool.GetArena(idx2);
        
        arena1.System.Initialize();
        arena2.System.Initialize();

        // Modifica Slot 1
        arena1.Food.Set(10);
        
        // Verifica Slot 2 intatto
        That(arena2.Food.IsSet(10), Is.False, "Slot 2 food bitboard should not be affected by Slot 1.");
    }

    [Test]
    public void Reset_Should_Allow_Reuse_Of_Slots()
    {
        var layout = new SlotMemoryLayout(Area, QueueCapacity, SnakesCount);
        _pool = new SlotMemoryPool(MaxSlots, 0, SnakesCount, _lookups, layout);

        int idx1 = _pool.Allocate();
        
        // Scrivo dati sporchi
        var arenaOld = _pool.GetArena(idx1);
        arenaOld.Food.Set(50);

        _pool.Reset();

        int idxNew = _pool.Allocate();
        That(idxNew, Is.EqualTo(idx1), "Should reuse index 0.");
        
        // Nota: Il reset del pool NON pulisce la memoria (per performance), 
        // è compito dell'Arena.InitializeFromRequest farlo.
        // Qui verifichiamo solo che l'indice sia tornato indietro.
        var arenaNew = _pool.GetArena(idxNew);
        That(arenaNew.Food.IsSet(50), Is.True, "Memory should theoretically persist until explicit clear.");
    }
}