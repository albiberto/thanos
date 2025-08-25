using System;
using System.Runtime.InteropServices; // Necessario per NativeMemory
using System.Threading;

namespace Thanos.Memory;

// La classe deve essere marcata come 'unsafe' per permettere l'uso di puntatori
public sealed unsafe class WarMemoryPool : IDisposable
{
    private GameContext _context;
    
    // Sostituiamo Memory<byte> con un puntatore alla memoria non gestita
    private readonly byte* _basePointer;
    private readonly long _totalSize;
    
    private long _offset;
    private bool _disposed;

    public WarMemoryPool(in GameContext context, long maxNodes) // <-- Usiamo long per maxNodes
    {
        _context = context;
        _totalSize = context.Layout.WarSlotSize * maxNodes;

        _basePointer = (byte*)NativeMemory.AlignedAlloc((nuint)_totalSize, 64);
        NativeMemory.Clear(_basePointer, (nuint)_totalSize);
        
        _offset = 0;
        _disposed = false;
    }
    
    public MemorySlot GetNext(out bool full)
    {
        var slotSize = _context.Layout.WarSlotSize;
        var newOffset = Interlocked.Add(ref _offset, slotSize);
        
        // CONTROLLO FONDAMENTALE: Assicuriamoci di non superare la memoria allocata
        if (newOffset > _totalSize)
        {
            full = true;
            return new MemorySlot();
        }
        
        var startOffset = newOffset - slotSize;
        
        // 2. ACCESSO: Creiamo uno Span che "punta" a una sezione della nostra memoria non gestita.
        // Anche se il buffer totale è >2GB, ogni singolo Span che creiamo è piccolo.
        var slotPointer = _basePointer + startOffset;
        var slotSpan = new Span<byte>(slotPointer, slotSize);

        full = false;
        return new MemorySlot(slotSpan, ref _context);
    }

    public void Clear()
    {
        _offset = 0;
        // Opzionale: se vuoi azzerare fisicamente la memoria all'inizio di ogni turno
        // NativeMemory.Clear(_basePointer, (nuint)_totalSize);
    }
    
    public void Reset(in GameContext context) => _context = context;

    public void Dispose()
    {
        if (_disposed) return;
        
        // 3. RILASCIO: Liberiamo la memoria non gestita che avevamo allocato.
        // Se questo non viene chiamato, la memoria rimarrà occupata fino alla chiusura del processo.
        if (_basePointer != null)
        {
            NativeMemory.Free(_basePointer);
        }
        
        _disposed = true;
        // GC.SuppressFinalize(this) non è strettamente necessario per una classe sealed,
        // ma è una buona pratica se si implementa il pattern IDisposable completo.
    }
}