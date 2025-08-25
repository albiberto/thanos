using System;
using System.Runtime.InteropServices; // Necessario per NativeMemory
using System.Threading;

namespace Thanos.Memory;

// La classe deve essere marcata come 'unsafe' per permettere l'uso di puntatori
public unsafe sealed class WarMemoryPool : IDisposable
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
        _totalSize = (long)context.Layout.WarSlotSize * maxNodes;

        if (_totalSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxNodes), "La dimensione totale della memoria deve essere positiva.");
        }

        // 1. ALLOCAZIONE: Chiediamo la memoria direttamente al sistema operativo.
        // 'nuint' è un intero nativo (64-bit su sistemi a 64-bit), perfetto per superare i 2GB.
        _basePointer = (byte*)NativeMemory.AlignedAlloc((nuint)_totalSize, 64);
        
        // È buona norma pulire la memoria appena allocata.
        NativeMemory.Clear(_basePointer, (nuint)_totalSize);
        
        _offset = 0;
        _disposed = false;
    }
    
    public MemorySlot GetNext()
    {
        var slotSize = _context.Layout.WarSlotSize;
        var newOffset = Interlocked.Add(ref _offset, slotSize);
        
        // CONTROLLO FONDAMENTALE: Assicuriamoci di non superare la memoria allocata
        if (newOffset > _totalSize)
        {
            // Ripristina l'offset per evitare overflow futuri
            Interlocked.Add(ref _offset, -slotSize); 
            throw new OutOfMemoryException($"Il WarMemoryPool è pieno. Richiesti {slotSize} byte, ma non c'è spazio sufficiente.");
        }
        
        var startOffset = newOffset - slotSize;
        
        // 2. ACCESSO: Creiamo uno Span che "punta" a una sezione della nostra memoria non gestita.
        // Anche se il buffer totale è >2GB, ogni singolo Span che creiamo è piccolo.
        var slotPointer = _basePointer + startOffset;
        var slotSpan = new Span<byte>(slotPointer, slotSize);
        
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