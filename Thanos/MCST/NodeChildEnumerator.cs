using Thanos.Memory;

namespace Thanos.MCST;

public ref struct ChildEnumerator(int index, NodeMemoryPool pool)
{
    private NodeMemoryPool _pool = pool;

    public int Current { get; private set; } = -1;
    private int _next = index;
    
    public bool MoveNext()
    {
        if (_next == -1) return false;
        
        Current = _next;
        ref var currentNode = ref _pool[Current];
        _next = currentNode.NextSiblingIndex;
        return true;
    }
    

    public readonly ChildEnumerator GetEnumerator() => this;
}