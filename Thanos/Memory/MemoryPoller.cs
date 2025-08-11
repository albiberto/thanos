using System.Runtime.InteropServices;
using Thanos.MCST;
using Thanos.War;

namespace Thanos.Memory;

/// <summary>
/// Gestisce un grande blocco di memoria non gestita e lo distribuisce in "kit"
/// pronti per l'assemblaggio di nodi di simulazione.
/// </summary>
public sealed unsafe class MemoryPool
{
    private readonly byte* _poolPtr;
    private readonly MemoryLayout _layout;
    private readonly WarContext _context;

    private long _offset;

    /// <summary>
    /// Inizializza il pooler, calcolando i layout e allocando la memoria.
    /// </summary>
    public MemoryPool(byte* poolPtr, MemoryLayout layout, WarContext context)
    {
        _poolPtr = poolPtr;
        _layout = layout;
        _context = context;

        _offset = 0;
    }
    
    public uint GetContextMemory(out WarContext* context) => throw new NotImplementedException("Implementa la logica per ottenere il contesto di memoria.");
    
    public uint GetLayoutMemory(out WarContext* context) => throw new NotImplementedException("Implementa la logica per ottenere il contesto di memoria.");

    public bool TryGetNextKit(out MemoryKit kit) => throw new NotImplementedException("Implementa la logica per ottenere il prossimo kit di memoria.");

    public void Reset() => _offset = 0;
}