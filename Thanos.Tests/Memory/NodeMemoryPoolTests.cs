using System.Runtime.CompilerServices;
using Thanos.Memory;
using static NUnit.Framework.Assert;

namespace Thanos.Tests.Memory;

[TestFixture]
public class NodeMemoryPoolTests
{
    private NodeMemoryPool? _pool;
    private const uint MaxNodes = 100;

    [TearDown]
    public void TearDown() => _pool?.Dispose();

    /// <summary>
    ///     Verifies that when the pool is configured with firstIndex=1,
    ///     allocation starts from index 1 (standard tree behavior with null root).
    /// </summary>
    [Test]
    public void Constructor_WithIndex1_Should_StartAllocating_From_1()
    {
        const int firstIndex = 1;
        _pool = new NodeMemoryPool(MaxNodes, firstIndex, new NodeMemoryLayout());

        var actualIndex = _pool.Allocate();
        var actualCount = _pool.Index;

        Multiple(() =>
        {
            That(actualCount, Is.EqualTo(2), 
                $"Index should be 2 after first allocation but was {actualCount}.");
            That(actualIndex, Is.EqualTo(1), 
                $"First allocation should return index 1 but was {actualIndex}.");
        });
    }

    /// <summary>
    ///     Verifies that when the pool is configured with firstIndex=0,
    ///     allocation starts from index 0 (zero-based array behavior).
    /// </summary>
    [Test]
    public void Constructor_WithIndex0_Should_StartAllocating_From_0()
    {
        const int firstIndex = 0;
        _pool = new NodeMemoryPool(MaxNodes, firstIndex, new NodeMemoryLayout());

        var actualIndex = _pool.Allocate();
        var actualCount = _pool.Index;

        Multiple(() =>
        {
            That(actualCount, Is.EqualTo(1), 
                $"Index should be 1 after first allocation but was {actualCount}.");
            That(actualIndex, Is.EqualTo(0), 
                $"First allocation should return index 0 but was {actualIndex}.");
        });
    }

    /// <summary>
    ///     Verifies that Reset() rewinds the Index to the configured firstIndex,
    ///     regardless of the starting value.
    /// </summary>
    [Test]
    public void Reset_Should_Rewind_Index_To_Configured_FirstIndex()
    {
        int startIdx = 5;
        _pool = new NodeMemoryPool(MaxNodes, startIdx, new NodeMemoryLayout());

        _pool.Allocate();
        _pool.Allocate();

        _pool.Reset();

        var actualIndex = _pool.Index;
        var actualFirstAllocAfterReset = _pool.Allocate();

        Multiple(() =>
        {
            That(actualIndex, Is.EqualTo(startIdx), 
                $"Index should be reset to {startIdx} but was {actualIndex}.");
            That(actualFirstAllocAfterReset, Is.EqualTo(startIdx), 
                $"First allocation after reset should return {startIdx} but was {actualFirstAllocAfterReset}.");
        });
    }

    /// <summary>
    ///     Verifies that the pool respects the stride specified in the NodeMemoryLayout,
    ///     ensuring correct distance between consecutive nodes in memory.
    /// </summary>
    [Test]
    public unsafe void Pool_Should_Respect_Stride_In_Layout()
    {
        var layout = new NodeMemoryLayout();
        _pool = new NodeMemoryPool(MaxNodes, 1, layout);

        _pool.Allocate();
        _pool.Allocate();

        ref var node1 = ref _pool.Get(1);
        ref var node2 = ref _pool.Get(2);

        var p1 = (byte*)Unsafe.AsPointer(ref node1);
        var p2 = (byte*)Unsafe.AsPointer(ref node2);
        var actualDistance = (nuint)(p2 - p1);
        var expectedDistance = layout.Node.Next;

        That(actualDistance, Is.EqualTo(expectedDistance), 
            $"Pool stride should be {expectedDistance} but was {actualDistance}.");
    }

    /// <summary>
    ///     Verifies that data written to a node persists correctly when read back,
    ///     ensuring proper memory management.
    /// </summary>
    [Test]
    public unsafe void ReadWrite_Should_Persist_Data()
    {
        _pool = new NodeMemoryPool(MaxNodes, 1, new NodeMemoryLayout());

        var index = _pool.Allocate();

        ref var node = ref _pool.Get(index);
        node.Visits = 42;
        node.Rewards[0] = 99.9f;

        ref var sameNode = ref _pool.Get(index);
        var actualVisits = sameNode.Visits;
        var actualReward = sameNode.Rewards[0];

        Multiple(() =>
        {
            That(actualVisits, Is.EqualTo(42), 
                $"Visits should be 42 but was {actualVisits}.");
            That(actualReward, Is.EqualTo(99.9f), 
                $"Rewards[0] should be 99.9 but was {actualReward}.");
        });
    }

    /// <summary>
    ///     Verifies that when the pool is full, Allocate() returns -1
    ///     to indicate allocation failure.
    /// </summary>
    [Test]
    public void Allocate_WhenFull_Should_Return_MinusOne()
    {
        _pool = new NodeMemoryPool(2, 0, new NodeMemoryLayout());

        var actualIdx1 = _pool.Allocate();
        var actualIdx2 = _pool.Allocate();
        var actualIdx3 = _pool.Allocate();

        Multiple(() =>
        {
            That(actualIdx1, Is.EqualTo(0), 
                $"First allocation should return index 0 but was {actualIdx1}.");
            That(actualIdx2, Is.EqualTo(1), 
                $"Second allocation should return index 1 but was {actualIdx2}.");
            That(actualIdx3, Is.EqualTo(-1), 
                $"Third allocation should fail and return -1 but was {actualIdx3}.");
        });
    }

    /// <summary>
    ///     Verifies that the pool initializes with correct Capacity and Index values,
    ///     ensuring proper construction.
    /// </summary>
    [Test]
    public void Constructor_Should_Initialize_Capacity_And_Index()
    {
        const uint expectedCapacity = 50;
        const int expectedFirstIndex = 1;
        
        _pool = new NodeMemoryPool(expectedCapacity, expectedFirstIndex, new NodeMemoryLayout());

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
    ///     Verifies that Get() returns references to different nodes at different indices,
    ///     ensuring proper memory layout and no aliasing.
    /// </summary>
    [Test]
    public unsafe void Get_Should_Return_Different_Nodes_At_Different_Indices()
    {
        _pool = new NodeMemoryPool(MaxNodes, 0, new NodeMemoryLayout());

        ref var node0 = ref _pool.Get(0);
        ref var node1 = ref _pool.Get(1);

        var p0 = Unsafe.AsPointer(ref node0);
        var p1 = Unsafe.AsPointer(ref node1);
        var arePointersDifferent = p0 != p1;

        That(arePointersDifferent, Is.True, 
            $"Get(0) and Get(1) should return different memory addresses but both pointed to same location.");
    }

    /// <summary>
    ///     Verifies that Dispose() executes successfully without throwing exceptions,
    ///     ensuring proper resource cleanup.
    /// </summary>
    [Test]
    public void Dispose_Should_Run_Without_Exceptions()
    {
        _pool = new NodeMemoryPool(MaxNodes, 0, new NodeMemoryLayout());

        DoesNotThrow(() => _pool.Dispose(), 
            "Dispose should not throw exceptions.");
        
        // Set to null to avoid double dispose in TearDown
        _pool = null;
    }
}

