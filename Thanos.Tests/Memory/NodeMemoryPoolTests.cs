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
        const byte firstIndex = 1;
        _pool = new NodeMemoryPool(MaxNodes, firstIndex, NodeMemoryLayout.Default);

        var idx = _pool.Allocate();

        using (EnterMultipleScope())
        {
            That(_pool.Count, Is.EqualTo(2), "Count should be 2 after first allocation.");
            That(idx, Is.EqualTo(1), "First allocation should return index 1.");
        }
    }

    /// <summary>
    ///     Verifies that when the pool is configured with firstIndex=0,
    ///     allocation starts from index 0 (zero-based array behavior).
    /// </summary>
    [Test]
    public void Constructor_WithIndex0_Should_StartAllocating_From_0()
    {
        const byte firstIndex = 0;
        _pool = new NodeMemoryPool(MaxNodes, firstIndex, NodeMemoryLayout.Default);

        var idx = _pool.Allocate();

        using (EnterMultipleScope())
        {
            That(_pool.Count, Is.EqualTo(1), "Count should be 1 after first allocation.");
            That(idx, Is.EqualTo(0), "First allocation should return index 0.");
        }
    }

    /// <summary>
    ///     Verifies that Reset() rewinds the Count to the configured firstIndex,
    ///     regardless of the starting value.
    /// </summary>
    [Test]
    public void Reset_Should_Rewind_Count_To_Configured_FirstIndex()
    {
        byte startIdx = 5;
        _pool = new NodeMemoryPool(MaxNodes, startIdx, NodeMemoryLayout.Default);

        _pool.Allocate();
        _pool.Allocate();

        _pool.Reset();

        using (EnterMultipleScope())
        {
            That(_pool.Count, Is.EqualTo(startIdx), $"Count should be reset to {startIdx}.");
            That(_pool.Allocate(), Is.EqualTo(startIdx), $"First allocation after reset should return {startIdx}.");
        }
    }

    /// <summary>
    ///     Verifies that the pool respects the padding specified in the NodeMemoryLayout,
    ///     ensuring correct stride between consecutive nodes in memory.
    /// </summary>
    [Test]
    public void Constructor_Should_Respect_Padding_In_Layout()
    {
        var paddedLayout = new NodeMemoryLayout(64);
        _pool = new NodeMemoryPool(MaxNodes, 1, paddedLayout);

        _pool.Allocate();
        _pool.Allocate();

        ref var node1 = ref _pool.Get(1);
        ref var node2 = ref _pool.Get(2);

        unsafe
        {
            var p1 = (byte*)Unsafe.AsPointer(ref node1);
            var p2 = (byte*)Unsafe.AsPointer(ref node2);
            var distance = p2 - p1;

            That(distance, Is.EqualTo(64), "Pool should respect the layout stride (padding).");
        }
    }

    /// <summary>
    ///     Verifies that data written to a node persists correctly when read back,
    ///     ensuring proper memory management.
    /// </summary>
    [Test]
    public void ReadWrite_Should_Persist_Data()
    {
        _pool = new NodeMemoryPool(MaxNodes, 1, NodeMemoryLayout.Default);

        var index = _pool.Allocate();

        ref var node = ref _pool[index];
        node.Visits = 42;
        unsafe
        {
            node.Rewards[0] = 99.9f;
        }

        ref var sameNode = ref _pool[index];

        using (EnterMultipleScope())
        {
            That(sameNode.Visits, Is.EqualTo(42), "Visits should persist correctly.");
            unsafe
            {
                That(sameNode.Rewards[0], Is.EqualTo(99.9f), "Rewards[0] should persist correctly.");
            }
        }
    }

    /// <summary>
    ///     Verifies that when the pool is full, Allocate() returns -1
    ///     to indicate allocation failure.
    /// </summary>
    [Test]
    public void Allocate_WhenFull_Should_Return_MinusOne()
    {
        _pool = new NodeMemoryPool(2, 0, NodeMemoryLayout.Default);

        var idx1 = _pool.Allocate();
        var idx2 = _pool.Allocate();
        var idx3 = _pool.Allocate();

        using (EnterMultipleScope())
        {
            That(idx1, Is.EqualTo(0), "First allocation should return index 0.");
            That(idx2, Is.EqualTo(1), "Second allocation should return index 1.");
            That(idx3, Is.EqualTo(-1), "Third allocation should fail and return -1.");
        }
    }
}
