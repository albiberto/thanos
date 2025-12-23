using Thanos.Memory;
using Thanos.War;
using static NUnit.Framework.Assert;

namespace Thanos.Tests.Memory;

[TestFixture]
public class SlotMemoryPoolTests
{
    private SlotMemoryPool? _pool;
    private LookupsMemoryPool _lookups;
    
    private const uint MaxSlots = 5;
    private const byte SnakesCount = 4;
    private const ushort Area = 121; 
    private const ushort QueueCapacity = 128;

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
    public void Allocate_WhenPoolHasCapacity_ThenReturnsSequentialIndices()
    {
        var layout = new SlotMemoryLayout(Area, QueueCapacity, SnakesCount);
        _pool = new SlotMemoryPool(MaxSlots, 0, SnakesCount, _lookups, layout);

        var idx1 = _pool.Allocate();
        var idx2 = _pool.Allocate();

        Multiple(() =>
        {
            That(idx1, Is.EqualTo(0));
            That(idx2, Is.EqualTo(1));
            That(_pool.Index, Is.EqualTo(2));
        });
    }

    [Test]
    public void Allocate_WhenPoolIsFull_ThenReturnsMinusOne()
    {
        var layout = new SlotMemoryLayout(Area, QueueCapacity, SnakesCount);
        _pool = new SlotMemoryPool(1, 0, SnakesCount, _lookups, layout);

        _pool.Allocate(); // 0
        var failed = _pool.Allocate(); // Full

        That(failed, Is.EqualTo(-1));
    }

    [Test]
    public void GetArena_WhenAccessed_ThenMapsCorrectlyToMemory()
    {
        var layout = new SlotMemoryLayout(Area, QueueCapacity, SnakesCount);
        _pool = new SlotMemoryPool(MaxSlots, 0, SnakesCount, _lookups, layout);

        var idx = _pool.Allocate();
        var arena = _pool.GetArena(idx);

        arena.System.Initialize();
        arena.System[0].Kill(); 
        
        // Lettura diretta senza lambda
        That(arena.System[0].IsDead, Is.True, "Arena wrapper should write to underlying memory.");
    }

    [Test]
    public void Memory_WhenWritingToSlot0_ThenSlot1RemainsUntouched()
    {
        var layout = new SlotMemoryLayout(Area, QueueCapacity, SnakesCount);
        _pool = new SlotMemoryPool(MaxSlots, 0, SnakesCount, _lookups, layout);

        var idx0 = _pool.Allocate();
        var idx1 = _pool.Allocate();

        var arena0 = _pool.GetArena(idx0);
        var arena1 = _pool.GetArena(idx1);

        arena0.Food.Set(50);
        
        // Lettura diretta senza lambda
        That(arena1.Food.IsSet(50), Is.False, "Memory bleed detected! Slot 0 writes affected Slot 1.");
    }

    [Test]
    public void InternalMemory_WhenWritingToFood_ThenHazardsAreUntouched()
    {
        var layout = new SlotMemoryLayout(Area, QueueCapacity, SnakesCount);
        _pool = new SlotMemoryPool(MaxSlots, 0, SnakesCount, _lookups, layout);

        var idx = _pool.Allocate();
        var arena = _pool.GetArena(idx);

        arena.Food.Set(10);

        // ESTRAZIONE VALORI (Fix per "Cannot use ref struct in lambda")
        // Leggiamo i valori booleani prima di entrare nella lambda
        var isFoodSet = arena.Food.IsSet(10);
        var isHazardsSet = arena.Hazards.IsSet(10);
        var isSnakesSet = arena.Snakes.IsSet(10);

        Multiple(() =>
        {
            That(isFoodSet, Is.True, "Food bit was not set.");
            That(isHazardsSet, Is.False, "Hazards memory overlaps with Food memory!");
            That(isSnakesSet, Is.False, "Snakes memory overlaps with Food memory!");
        });
    }

    [Test]
    public void Views_WhenArenaUpdatesMemory_ThenHeuristicsSeesChanges()
    {
        var layout = new SlotMemoryLayout(Area, QueueCapacity, SnakesCount);
        _pool = new SlotMemoryPool(MaxSlots, 0, SnakesCount, _lookups, layout);

        var idx = _pool.Allocate();
        
        var arena = _pool.GetArena(idx);
        var heuristics = _pool.GetHeuristics(idx);

        arena.System[0].Kill();
        
        var outcome = heuristics.Outcome(0);

        That(outcome, Is.EqualTo(-1.0f), "Heuristics view is detached from Arena memory.");
    }

    [Test]
    public void Reset_WhenCalled_ThenRewindsIndexAllowingReuse()
    {
        var layout = new SlotMemoryLayout(Area, QueueCapacity, SnakesCount);
        _pool = new SlotMemoryPool(MaxSlots, 5, SnakesCount, _lookups, layout);

        _pool.Allocate(); 
        _pool.Reset();

        var newIdx = _pool.Allocate();
        That(newIdx, Is.EqualTo(5), "Reset should rewind allocator to StartIndex.");
    }

    [Test]
    public void Dispose_WhenCalledTwice_ThenDoesNotThrow()
    {
        var layout = new SlotMemoryLayout(Area, QueueCapacity, SnakesCount);
        var pool = new SlotMemoryPool(1, 0, SnakesCount, _lookups, layout);

        pool.Dispose();
        DoesNotThrow(() => pool.Dispose());
    }
}