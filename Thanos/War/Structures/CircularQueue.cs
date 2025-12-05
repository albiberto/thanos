using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Thanos.War.Structures;

public ref struct CircularQueue(Span<byte> raw, ref CircularQueueState state, ushort capacity)
{
    // Casting zero-copy da byte a ushort
    public Span<ushort> Buffer { get; } = MemoryMarshal.Cast<byte, ushort>(raw);
    
    // Riferimento allo stato mutabile (Head, Tail, Length)
    public ref CircularQueueState _state = ref state;
    
    // Mask calcolata al volo: capacity deve essere potenza di 2 (es. 128 -> mask 127)
    // La salviamo nel ref struct così evitiamo di ricalcolarla ad ogni accesso
    private readonly int _wrapMask = capacity - 1;

    // Esposizione Length dallo stato
    public readonly int Length => _state.Length;

    // --- PEEKING (Lettura senza modifica) ---
    
    // HeadIndex punta al PROSSIMO slot libero. L'ultimo elemento inserito è Head - 1.
    public ushort PeekHead => Buffer[(_state.HeadIndex - 1) & _wrapMask];
    
    public ushort PeekTail => Buffer[_state.TailIndex & _wrapMask];
    
    public ushort PeekElementBeforeTail => Buffer[(_state.TailIndex + 1) & _wrapMask];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Enqueue(ushort value)
    {
        // Scriviamo usando la maschera (gestione wrap-around automatica per potenze di 2)
        Buffer[_state.HeadIndex & _wrapMask] = value;
        _state.AdvanceHead();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort Dequeue()
    {
        // Leggiamo usando la maschera
        var value = Buffer[_state.TailIndex & _wrapMask];
        _state.AdvanceTail();
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        // Pulisce il buffer (opzionale, ma utile per debug)
        Buffer.Clear();
        // Resetta indici a 0
        _state.Reset();
    }
}