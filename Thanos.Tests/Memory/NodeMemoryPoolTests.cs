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
    public void TearDown()
    {
        _pool?.Dispose();
        _pool = null;
    }

    [Test]
    public void Constructor_WithIndex1_Should_StartAllocating_From_1()
    {
        const int firstIndex = 1;
        _pool = new NodeMemoryPool(MaxNodes, firstIndex, new NodeMemoryLayout());

        var actualIndex = _pool.Allocate();
        var actualCount = _pool.Index;

        Multiple(() =>
        {
            That(actualCount, Is.EqualTo(2), "Index should be 2 after first allocation.");
            That(actualIndex, Is.EqualTo(1), "First allocation should return index 1.");
        });
    }

    [Test]
    public void Reset_Should_Rewind_Index_To_Configured_FirstIndex()
    {
        const int startIdx = 5;
        _pool = new NodeMemoryPool(MaxNodes, startIdx, new NodeMemoryLayout());

        _pool.Allocate(); // 5
        _pool.Allocate(); // 6

        _pool.Reset();

        var actualIndex = _pool.Index;
        var actualFirstAllocAfterReset = _pool.Allocate();

        Multiple(() =>
        {
            That(actualIndex, Is.EqualTo(startIdx + 1), "Index should be incremented after allocation.");
            That(actualFirstAllocAfterReset, Is.EqualTo(startIdx), "First allocation after reset should return startIdx.");
        });
    }

    [Test]
    public unsafe void Pool_Should_Respect_Stride_In_Layout()
    {
        var layout = new NodeMemoryLayout();
        _pool = new NodeMemoryPool(MaxNodes, 1, layout);

        ref var node1 = ref _pool.Get(1);
        ref var node2 = ref _pool.Get(2);

        var p1 = (byte*)Unsafe.AsPointer(ref node1);
        var p2 = (byte*)Unsafe.AsPointer(ref node2);
        
        var actualDistance = (long)(p2 - p1);
        var expectedDistance = (long)layout.Node.Next;

        That(actualDistance, Is.EqualTo(expectedDistance), 
            $"Pool stride should be {expectedDistance} but was {actualDistance}.");
    }

    [Test]
    public unsafe void ReadWrite_Should_Persist_Data_In_FixedBuffer()
    {
        _pool = new NodeMemoryPool(MaxNodes, 1, new NodeMemoryLayout());

        var index = _pool.Allocate();

        ref var node = ref _pool.Get(index);
        node.Visits = 42;
        
        // Accesso unsafe al buffer fixed
        node.Rewards[0] = 99.9f;
        node.Rewards[3] = 1.23f;

        ref var sameNode = ref _pool.Get(index);
        
            That(sameNode.Visits, Is.EqualTo(42));
            That(sameNode.Rewards[0], Is.EqualTo(99.9f));
            That(sameNode.Rewards[3], Is.EqualTo(1.23f));
    }

    [Test]
    public void Allocate_WhenFull_Should_Return_MinusOne()
    {
        _pool = new NodeMemoryPool(2, 0, new NodeMemoryLayout());

        _pool.Allocate(); // 0
        _pool.Allocate(); // 1
        var failedIdx = _pool.Allocate(); // Full

        That(failedIdx, Is.EqualTo(-1), "Should return -1 when capacity is exhausted.");
    }
}