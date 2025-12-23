using System.Runtime.CompilerServices;
using Thanos.MCST;
using Thanos.Memory;
using static NUnit.Framework.Assert;

namespace Thanos.Tests.Memory;

[TestFixture]
public class NodeMemoryPoolTests
{
    private NodeMemoryPool? _pool;
    private const uint MaxNodes = 100;
    private readonly NodeMemoryLayout _layout = new();

    [TearDown]
    public void TearDown()
    {
        _pool?.Dispose();
        _pool = null;
    }

    [Test]
    public void Allocate_WhenFirstIndexIsOne_ThenStartsAllocationFromOne()
    {
        const int firstIndex = 1;
        _pool = new NodeMemoryPool(MaxNodes, firstIndex, in _layout);

        var actualIndex = _pool.Allocate();
        var internalCounter = _pool.Index;

        Multiple(() =>
        {
            That(actualIndex, Is.EqualTo(1));
            That(internalCounter, Is.EqualTo(2));
        });
    }

    [Test]
    public void Allocate_WhenPoolIsFull_ThenReturnsMinusOne()
    {
        _pool = new NodeMemoryPool(2, 0, in _layout); // Capacità 2

        _pool.Allocate(); // 0
        _pool.Allocate(); // 1
        var failedIdx = _pool.Allocate(); // Full

        That(failedIdx, Is.EqualTo(-1));
    }

    [Test]
    public unsafe void Get_WhenAccessingSequentialNodes_ThenRespectsMemoryStride()
    {
        _pool = new NodeMemoryPool(MaxNodes, 1, in _layout);
        var idx1 = _pool.Allocate();
        var idx2 = _pool.Allocate();

        ref var node1 = ref _pool.Get(idx1);
        ref var node2 = ref _pool.Get(idx2);

        var p1 = (byte*)Unsafe.AsPointer(ref node1);
        var p2 = (byte*)Unsafe.AsPointer(ref node2);
        
        var actualDistance = (long)(p2 - p1);
        var expectedDistance = (long)_layout.Node.Next;

        That(actualDistance, Is.EqualTo(expectedDistance), 
            "Memory stride mismatch. Nodes are not aligned correctly.");
    }

    [Test]
    public unsafe void Get_WhenWritingToNode_ThenPersistsDataInMemory()
    {
        _pool = new NodeMemoryPool(MaxNodes, 1, in _layout);
        var index = _pool.Allocate();

        ref var node = ref _pool.Get(index);
        
        node.Visits = 999;
        node.Rewards[0] = 123.456f;
        node.Rewards[3] = -10.0f;

        // Rilettura
        ref var sameNode = ref _pool.Get(index);

        // FIX: Estrazione valori prima della Lambda
        var actualVisits = sameNode.Visits;
        var actualReward0 = sameNode.Rewards[0];
        var actualReward3 = sameNode.Rewards[3];
        
        Multiple(() =>
        {
            That(actualVisits, Is.EqualTo(999));
            That(actualReward0, Is.EqualTo(123.456f));
            That(actualReward3, Is.EqualTo(-10.0f));
        });
    }

    [Test]
    public unsafe void Memory_WhenWritingToNode1_ThenNode2RemainsUntouched()
    {
        // TEST CRITICO DI ISOLAMENTO
        // Verifica che non ci siano sovrapposizioni di memoria tra nodi adiacenti
        _pool = new NodeMemoryPool(MaxNodes, 1, in _layout);
        var idx1 = _pool.Allocate();
        var idx2 = _pool.Allocate();

        ref var node1 = ref _pool.Get(idx1);
        ref var node2 = ref _pool.Get(idx2);

        // Scriviamo valori "sentinella" nel nodo 1 (anche alla fine della struct)
        node1.Visits = 111;
        node1.Rewards[0] = 999f;
        
        // Verifichiamo che il nodo 2 sia ancora pulito (0)
        // FIX: Estrazione valori
        var n2Visits = node2.Visits;
        var n2Reward = node2.Rewards[0];

        Multiple(() =>
        {
            That(n2Visits, Is.EqualTo(0), "Memory Bleed! Node 1 overwrote Node 2's Visits.");
            That(n2Reward, Is.EqualTo(0f), "Memory Bleed! Node 1 overwrote Node 2's Rewards.");
        });
    }

    [Test]
    public void Reset_WhenCalled_ThenRewindsIndexAllowingReuse_WithDirtyMemory()
    {
        // Verifica che il Reset non pulisca la memoria (per performance),
        // ma permetta di riutilizzare l'indice.
        const int startIdx = 5;
        _pool = new NodeMemoryPool(MaxNodes, startIdx, in _layout);

        var idx = _pool.Allocate(); // 5
        ref var node = ref _pool.Get(idx);
        node.Visits = 555; // Sporchiamo la memoria

        _pool.Reset();

        var reusedIndex = _pool.Allocate(); // Deve ridarci 5
        ref var reusedNode = ref _pool.Get(reusedIndex);

        // FIX: Estrazione valori
        var indexMatch = reusedIndex == startIdx;
        var isDirty = reusedNode.Visits == 555;

        Multiple(() =>
        {
            That(indexMatch, Is.True, "Allocator did not rewind.");
            That(isDirty, Is.True, "Pool should NOT clear memory on Reset (Engine's job).");
        });
    }

    [Test]
    public void Dispose_WhenCalledTwice_ThenDoesNotThrow()
    {
        _pool = new NodeMemoryPool(10, 0, in _layout);
        _pool.Dispose();
        DoesNotThrow(() => _pool.Dispose());
    }
}