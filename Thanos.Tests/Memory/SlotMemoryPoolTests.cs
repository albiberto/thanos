using Thanos.Memory;
using static NUnit.Framework.Assert;

namespace Thanos.Tests.Memory;

[TestFixture]
public class SlotMemoryPoolTests
{
    private SlotMemoryPool? _pool;
    private LookupsMemoryPool _lookups;
    private const uint MaxSlots = 10;
    private const byte SnakesCount = 2;
    private const ushort Area = 121; // 11x11
    private const ushort QueueCapacity = 121;

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

    /// <summary>
    ///     Verifies that SlotMemoryPool constructor initializes Capacity and Index correctly,
    ///     with Index starting at the configured firstIndex value.
    /// </summary>
    [Test]
    public void Constructor_Should_Initialize_Capacity_And_Index()
    {
        const uint expectedCapacity = MaxSlots;
        const int expectedFirstIndex = 0;

        var layout = new SlotMemoryLayout(Area, QueueCapacity, SnakesCount);
        _pool = new SlotMemoryPool(expectedCapacity, expectedFirstIndex, SnakesCount, _lookups, layout);

        var actualCapacity = _pool.Capacity;
        var actualIndex = _pool.Index;

        Multiple(() =>
        {
            That(actualCapacity, Is.EqualTo(expectedCapacity),
                $"Capacity should be {expectedCapacity} but was {actualCapacity}.");
            That(actualIndex, Is.EqualTo(expectedFirstIndex),
                $"Index should start at {expectedFirstIndex} but was {actualIndex}.");
        });
    }

    /// <summary>
    ///     Verifies that SlotMemoryPool.Allocate() returns sequential indices starting from firstIndex
    ///     and correctly increments the Index counter.
    /// </summary>
    [Test]
    public void Allocate_Should_Return_SequentialIndices()
    {
        const int firstIndex = 0;
        var layout = new SlotMemoryLayout(Area, QueueCapacity, SnakesCount);
        _pool = new SlotMemoryPool(MaxSlots, firstIndex, SnakesCount, _lookups, layout);

        var actualIdx1 = _pool.Allocate();
        var actualIdx2 = _pool.Allocate();
        var actualIndex = _pool.Index;

        Multiple(() =>
        {
            That(actualIdx1, Is.EqualTo(0),
                $"First allocation should return 0 but was {actualIdx1}.");
            That(actualIdx2, Is.EqualTo(1),
                $"Second allocation should return 1 but was {actualIdx2}.");
            That(actualIndex, Is.EqualTo(2),
                $"Index should be 2 after two allocations but was {actualIndex}.");
        });
    }

    /// <summary>
    ///     Verifies that SlotMemoryPool.Allocate() with non-zero firstIndex
    ///     starts allocating from that index correctly.
    /// </summary>
    [Test]
    public void Allocate_Should_Start_From_ConfiguredFirstIndex()
    {
        const int firstIndex = 5;
        var layout = new SlotMemoryLayout(Area, QueueCapacity, SnakesCount);
        _pool = new SlotMemoryPool(MaxSlots, firstIndex, SnakesCount, _lookups, layout);

        var actualFirstAlloc = _pool.Allocate();
        var actualIndex = _pool.Index;

        Multiple(() =>
        {
            That(actualFirstAlloc, Is.EqualTo(firstIndex),
                $"First allocation should return {firstIndex} but was {actualFirstAlloc}.");
            That(actualIndex, Is.EqualTo(firstIndex + 1),
                $"Index should be {firstIndex + 1} after first allocation but was {actualIndex}.");
        });
    }

    /// <summary>
    ///     Verifies that SlotMemoryPool.Reset() rewinds the Index to the configured firstIndex,
    ///     allowing slot reuse.
    /// </summary>
    [Test]
    public void Reset_Should_Rewind_Index_To_FirstIndex()
    {
        const int firstIndex = 0;
        var layout = new SlotMemoryLayout(Area, QueueCapacity, SnakesCount);
        _pool = new SlotMemoryPool(MaxSlots, firstIndex, SnakesCount, _lookups, layout);

        _pool.Allocate();
        _pool.Allocate();

        _pool.Reset();

        var actualIndex = _pool.Index;
        var actualFirstAllocAfterReset = _pool.Allocate();

        Multiple(() =>
        {
            That(actualIndex, Is.EqualTo(firstIndex),
                $"Index should be reset to {firstIndex} but was {actualIndex}.");
            That(actualFirstAllocAfterReset, Is.EqualTo(firstIndex),
                $"First allocation after reset should return {firstIndex} but was {actualFirstAllocAfterReset}.");
        });
    }

    /// <summary>
    ///     Verifies that when the pool is full, Allocate() returns -1
    ///     to indicate allocation failure.
    /// </summary>
    [Test]
    public void Allocate_WhenFull_Should_Return_MinusOne()
    {
        const uint capacity = 2;
        const int firstIndex = 0;
        var layout = new SlotMemoryLayout(Area, QueueCapacity, SnakesCount);
        _pool = new SlotMemoryPool(capacity, firstIndex, SnakesCount, _lookups, layout);

        var actualIdx1 = _pool.Allocate();
        var actualIdx2 = _pool.Allocate();
        var actualIdx3 = _pool.Allocate();

        Multiple(() =>
        {
            That(actualIdx1, Is.EqualTo(0),
                $"First allocation should return 0 but was {actualIdx1}.");
            That(actualIdx2, Is.EqualTo(1),
                $"Second allocation should return 1 but was {actualIdx2}.");
            That(actualIdx3, Is.EqualTo(-1),
                $"Third allocation should fail and return -1 but was {actualIdx3}.");
        });
    }

    /// <summary>
    ///     Verifies that SlotMemoryPool.GetArena() returns an Arena with the correct snake count
    ///     as configured during pool construction.
    /// </summary>
    [Test]
    public void GetArena_Should_Return_Arena_With_CorrectSnakeCount()
    {
        const int expectedSnakeCount = 3;
        var layout = new SlotMemoryLayout(Area, QueueCapacity, expectedSnakeCount);
        _pool = new SlotMemoryPool(MaxSlots, 0, expectedSnakeCount, _lookups, layout);

        int slotIndex = _pool.Allocate();
        var arena = _pool.GetArena(slotIndex);

        var actualSnakeCount = arena.System.Count;

        That(actualSnakeCount, Is.EqualTo(expectedSnakeCount),
            $"Arena.System.Count should be {expectedSnakeCount} but was {actualSnakeCount}.");
    }

    /// <summary>
    ///     Verifies that different slots in the pool do not overlap in memory,
    ///     ensuring that modifications to one slot do not affect another slot.
    /// </summary>
    [Test]
    public void Slots_Should_Not_Overlap()
    {
        var layout = new SlotMemoryLayout(Area, QueueCapacity, SnakesCount);
        _pool = new SlotMemoryPool(MaxSlots, 0, SnakesCount, _lookups, layout);

        int idx1 = _pool.Allocate();
        int idx2 = _pool.Allocate();

        var arena1 = _pool.GetArena(idx1);
        var arena2 = _pool.GetArena(idx2);

        // Initialize systems to set up queues properly
        arena1.System.Initialize();
        arena2.System.Initialize();

        // Set a bit in arena1's food bitboard
        arena1.Food.Set(42);

        // Verify arena2's food bitboard is not affected
        var arena2HasBit42 = arena2.Food.IsSet(42);

        That(arena2HasBit42, Is.False,
            $"Arena2.Food should not have bit 42 set but it was set. Slots are overlapping.");
    }

    /// <summary>
    ///     Verifies that GetArena() called multiple times on the same slot index
    ///     returns views over the same underlying memory, allowing data persistence.
    /// </summary>
    [Test]
    public void GetArena_Should_Return_Views_Over_Same_Memory()
    {
        var layout = new SlotMemoryLayout(Area, QueueCapacity, SnakesCount);
        _pool = new SlotMemoryPool(MaxSlots, 0, SnakesCount, _lookups, layout);

        int slotIndex = _pool.Allocate();

        var arena1 = _pool.GetArena(slotIndex);
        arena1.System.Initialize();
        arena1.Food.Set(99);

        var arena2 = _pool.GetArena(slotIndex);
        var arena2HasBit99 = arena2.Food.IsSet(99);

        That(arena2HasBit99, Is.True,
            $"Arena2.Food should have bit 99 set (from Arena1) but it was not set. Views should share memory.");
    }


    /// <summary>
    ///     Verifies that SlotMemoryPool.Dispose() executes successfully without throwing exceptions,
    ///     ensuring proper resource cleanup.
    /// </summary>
    [Test]
    public void Dispose_Should_Run_Without_Exceptions()
    {
        var layout = new SlotMemoryLayout(Area, QueueCapacity, SnakesCount);
        _pool = new SlotMemoryPool(MaxSlots, 0, SnakesCount, _lookups, layout);

        DoesNotThrow(() => _pool.Dispose(),
            "Dispose should not throw exceptions.");

        // Set to null to avoid double dispose in TearDown
        _pool = null;
    }

    /// <summary>
    ///     Verifies that Arena.System provides access to individual snakes through indexer,
    ///     ensuring proper SnakesSystem functionality.
    /// </summary>
    [Test]
    public void Arena_System_Should_Provide_Snake_Access()
    {
        const int snakeCount = 4;
        var layout = new SlotMemoryLayout(Area, QueueCapacity, snakeCount);
        _pool = new SlotMemoryPool(MaxSlots, 0, snakeCount, _lookups, layout);

        int slotIndex = _pool.Allocate();
        var arena = _pool.GetArena(slotIndex);
        arena.System.Initialize();

        // Access each snake directly (can't use lambda with ref struct)
        var exceptionThrown = false;
        try
        {
            for (int i = 0; i < snakeCount; i++)
            {
                var snake = arena.System[i];
                var _ = snake.HP; // Access a property to ensure it works
            }
        }
        catch
        {
            exceptionThrown = true;
        }

        That(exceptionThrown, Is.False,
            "Accessing snakes through System indexer should not throw exceptions.");
    }
}

