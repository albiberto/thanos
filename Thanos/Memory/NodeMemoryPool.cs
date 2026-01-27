using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Thanos.Abstract;
using Thanos.MCST;

namespace Thanos.Memory;

public sealed unsafe class NodeMemoryPool : INodeMemoryPool
{
    private byte* _basePointer;
    private bool _disposed;

    private readonly int _firstIndex;
    private readonly nuint _stride; 

    // "The LightSpeed" Concurrency: Indice condiviso atomico
    private int _sharedIndex;

    public uint Capacity { get; }
    
    // Espone l'indice corrente (volatile read implicito su x64/ARM64 moderni o via interlocked se strict)
    public int Index => _sharedIndex;

    public NodeMemoryPool(uint capacity, int firstIndex, in NodeMemoryLayout layout)
    {
        _firstIndex = firstIndex;
        _sharedIndex = firstIndex; // Inizializzazione

        _stride = layout.Node.Next;
        
        Capacity = capacity;
        
        var totalSize = capacity * _stride;
        _basePointer = (byte*)NativeMemory.AlignedAlloc(totalSize, Constants.CacheLine);
        NativeMemory.Clear(_basePointer, totalSize);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref Node Get(int index) => ref Unsafe.AsRef<Node>(_basePointer + (nuint)index * _stride);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Allocate()
    {
        // Incremento atomico: Ritorna il valore POST-incremento
        var newIndex = Interlocked.Increment(ref _sharedIndex);
        var allocatedIndex = newIndex - 1; // Recuperiamo l'indice appena riservato

        if (allocatedIndex >= Capacity) return -1;
        return allocatedIndex;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int AllocateBatch(int count)
    {
        // Prenotazione di blocco atomica
        var newIndex = Interlocked.Add(ref _sharedIndex, count);
        var startIndex = newIndex - count; // L'inizio del blocco riservato

        if (startIndex >= Capacity) return -1;
        
        // Se il batch sfora la capacità, possiamo decidere se fallire o accettare parzialmente.
        // Per sicurezza e velocità, se sfora falliamo tutto il batch.
        if (newIndex > Capacity) 
        {
            // Opzionale: Si potrebbe fare rollback, ma in MCTS saturare significa fine partita.
            return -1; 
        }

        return startIndex;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Reset() => _sharedIndex = _firstIndex; // Reset semplice (assunto single-thread tra i match)

    public void Dispose()
    {
        if (_disposed || _basePointer == null) return;
        
        NativeMemory.AlignedFree(_basePointer);
        _basePointer = null;
        _disposed = true;
    }
}