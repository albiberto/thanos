using Thanos.War;

namespace Thanos.Memory;

public sealed unsafe class MemoryPool(byte* poolStartPtr)
{
    private long _currentOffset;
    
    private MemoryLayout _layout;
    private WarContext _context;

    /// <summary>
    /// Tenta di ottenere il prossimo slot di memoria in modo thread-safe.
    /// Può essere chiamato da più thread contemporaneamente senza rischi di corruzione dei dati.
    /// </summary>
    public bool TryGetNext(out MemorySlot builder)
    {
        // 1. Aggiunge atomicamente la dimensione dello slot all'offset corrente. 'newOffset' è il valore dell'offset DOPO l'aggiunta.
        var newOffset = Interlocked.Add(ref _currentOffset, _layout.Sizes.Slot);

        // 2. Controlla se abbiamo superato la capacità totale del pool. Se il nuovo offset è maggiore della dimensione del pool, l'allocazione fallisce.
        if (newOffset > (long)_layout.Sizes.Pool)
        {
            builder = default;
            return false;
        }

        // 3. Calcola l'offset di inizio del nostro slot, che è quello PRIMA dell'aggiunta.
        var startOffset = newOffset - _layout.Sizes.Slot;
    
        // 4. Calcola il puntatore alla nostra area di memoria.
        var currentSlotPtr = poolStartPtr + startOffset;

        // 5. Crea lo slot e restituisce successo.
        builder = new MemorySlot(currentSlotPtr, _context, _layout);
        return true;
    }

    public void Reset() => _currentOffset = 0;

    public void Reset(in WarContext context, in MemoryLayout layout)
    {
        Reset();

        _context = context;
        _layout = layout;
    }
}