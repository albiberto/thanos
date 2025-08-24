using System.Buffers;
using System.Runtime.InteropServices;
using System.Threading;
using Thanos.MCST; // Assumendo che Node sia qui

namespace Thanos.Memory;

/// <summary>
/// A high-performance, thread-safe memory pool for MCTS Node structs.
/// It pre-allocates a large, contiguous block of memory and hands out
/// indices to nodes, providing direct ref access via an indexer.
/// This design avoids heap allocations and garbage collection pressure.
/// </summary>
public sealed class NodeMemoryPool : IDisposable
{
    private readonly IMemoryOwner<byte> _memoryOwner;
    private readonly Memory<byte> _memory;
    private MemoryHandle _memoryHandle;
    private int _currentNodeIndex; // Usiamo un indice intero per l'allocazione, è più semplice del long offset.

    private GameContext _context; 

    public NodeMemoryPool(in GameContext context, int maxNodes = Constants.MaxNodes)
    {
        _context = context;
        
        _memoryOwner = MemoryPool<byte>.Shared.Rent(_context.Layout.Node.Size * maxNodes);
        _memory = _memoryOwner.Memory;
        _memoryHandle = _memory.Pin(); // Pin della memoria per accesso sicuro
        _currentNodeIndex = 0;
    }
    
    public int GetNextIndex()
    {
        var index = Interlocked.Increment(ref _currentNodeIndex) - 1;

        // TODO: Aggiungere un controllo per assicurarsi che 'index' non superi 'maxNodes'
        // if (index >= Constants.MaxNodes) throw new OutOfMemoryException("Node pool exhausted.");
        
        return index;
    }
    
    public ref Node this[int index]
    {
        get
        {
            // 1. Calcola l'offset in byte per il nodo richiesto.
            var nodeSize = _context.Layout.Node.Size;
            var offset = index * nodeSize;

            // 2. Ottieni lo Span<byte> che rappresenta esattamente la memoria di quel singolo nodo.
            var nodeSpan = _memory.Span.Slice(offset, nodeSize);

            // 3. Ottieni e restituisci un riferimento diretto e modificabile alla struct Node.
            return ref MemoryMarshal.GetReference(MemoryMarshal.Cast<byte, Node>(nodeSpan));
        }
    }

    public void Reset(in GameContext context)
    {
        // TODO: Aggiungere la ricerca dell'indice del nodo che ha lo statto hash per riutilizzo alberto
        // Attualmente resetta tutto, non sicuro che vada messo qui
        _context = context;
        _currentNodeIndex = 0;
    }
    
    public void Dispose()
    {
        _memoryOwner.Dispose();
        _memoryHandle.Dispose();
    }
}