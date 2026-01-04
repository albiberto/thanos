using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Thanos.Abstract;
using Thanos.War;
using Thanos.War.Structures;

namespace Thanos.Memory;

public sealed unsafe class SlotMemoryPool : ISlotMemoryPool
{
    private byte* _basePointer;
    private bool _disposed;

    private readonly int _firstIndex;
    private readonly int _snakesCount;
    private readonly nuint _stride; 
    private readonly SlotMemoryLayout _layout;
    private readonly ILookupsMemoryPool _lookupsMemoryPool;
    
    public uint Capacity { get; }
    public int Index { get; private set; }

    public SlotMemoryPool(uint capacity, int firstIndex, int snakesCount, ILookupsMemoryPool lookupsMemoryPool, in SlotMemoryLayout layout)
    {
        _firstIndex = firstIndex;
        _snakesCount = snakesCount;
        
        _layout = layout;
        // Lo stride (passo) tra uno slot e l'altro è definito dal layout.
        // Assicurati che SlotMemoryLayout.SlotStride.Next sia multiplo di 64 (cache line) nel Layout.
        _stride = layout.SlotStride.Next;
        
        _lookupsMemoryPool = lookupsMemoryPool;
        
        Capacity = capacity;
        Index = _firstIndex;
        
        var totalSize = _stride * (nuint)capacity;
        // Allocazione allineata fondamentale per le performance SIMD
        _basePointer = (byte*)NativeMemory.AlignedAlloc(totalSize, Constants.CacheLine);
        NativeMemory.Clear(_basePointer, totalSize);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Arena GetArena(int index)
    {
        // Costruiamo le view (ref struct) sulla memoria nativa "calda"
        BuildMemory(index, out var system, out var foodBitboard, out var hazardsBitboard, out var collisionsBitboard);
        return new Arena(system, foodBitboard, hazardsBitboard, collisionsBitboard, _lookupsMemoryPool.NeighborsMatrix);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Heuristics GetHeuristics(int index)
    {
        // Stessa memoria, diversa interpretazione (Heuristics vs Arena)
        BuildMemory(index, out var system, out var foodBitboard, out var hazardsBitboard, out var collisionsBitboard);
        return new Heuristics(system, foodBitboard, hazardsBitboard, collisionsBitboard, _lookupsMemoryPool.NeighborsMatrix, _lookupsMemoryPool.CoordinatesMatrix);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Allocate()
    {
        // Allocazione sequenziale lock-free (il pool è thread-local o usato in contesto safe)
        if (Index >= Capacity) return -1;
        return Index++;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Reset() => Index = _firstIndex;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void BuildMemory(int index, out SnakesSystem system, out Bitboard foodBitboard, out Bitboard hazardsBitboard, out Bitboard collisionsBitboard)
    {
        // Calcolo indirizzo base dello slot
        var slotPtr = _basePointer + (nuint)index * _stride;
        
        // SnakesSystem: Wrapper sui 4 serpenti
        system = new SnakesSystem(slotPtr, in _layout, _snakesCount);

        // Bitboards: Wrappers sui puntatori alle bitboard globali dello slot
        // Notare l'uso di Span temporanei per passare i puntatori ai costruttori Bitboard
        foodBitboard = new Bitboard(new Span<byte>(slotPtr + _layout.FoodBitboard.Offset, (int)_layout.FoodBitboard.Length));
        hazardsBitboard = new Bitboard(new Span<byte>(slotPtr + _layout.HazardsBitboard.Offset, (int)_layout.HazardsBitboard.Length));
        collisionsBitboard = new Bitboard(new Span<byte>(slotPtr + _layout.CollisionsBitboard.Offset, (int)_layout.CollisionsBitboard.Length));
    }

    public void Dispose()
    {
        if (_disposed || _basePointer is null) return;
        
        NativeMemory.AlignedFree(_basePointer);
        _basePointer = null;
        _disposed = true;
    }
}