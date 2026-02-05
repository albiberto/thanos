using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Thanos.Abstract;
using Thanos.War;
using Thanos.War.Brain;
using Thanos.War.Snake;
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
    
    // Concurrency
    private int _sharedIndex;

    public uint Capacity { get; }
    public int Index => _sharedIndex;

    public SlotMemoryPool(uint capacity, int firstIndex, int snakesCount, ILookupsMemoryPool lookupsMemoryPool, in SlotMemoryLayout layout)
    {
        _firstIndex = firstIndex;
        _sharedIndex = firstIndex;

        _snakesCount = snakesCount;
        _layout = layout;
        _stride = layout.SlotStride.Next;
        _lookupsMemoryPool = lookupsMemoryPool;
        
        Capacity = capacity;
        
        var totalSize = _stride * (nuint)capacity;
        _basePointer = (byte*)NativeMemory.AlignedAlloc(totalSize, Constants.CacheLine);
        NativeMemory.Clear(_basePointer, totalSize);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Arena GetArena(int index)
    {
        BuildMemory(index, out var system, out var foodBitboard, out var hazardsBitboard, out var collisionsBitboard);
        return new(system, foodBitboard, hazardsBitboard, collisionsBitboard, _lookupsMemoryPool.NeighborsMatrix);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Heuristics GetHeuristics(int index)
    {
        BuildMemory(index, out var system, out var foodBitboard, out var hazardsBitboard, out var collisionsBitboard);
        return new(system, foodBitboard, hazardsBitboard, collisionsBitboard, _lookupsMemoryPool.NeighborsMatrix, _lookupsMemoryPool.CoordinatesMatrix);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Allocate()
    {
        var newIndex = Interlocked.Increment(ref _sharedIndex);
        var allocatedIndex = newIndex - 1;

        if (allocatedIndex >= Capacity) return -1;
        return allocatedIndex;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int AllocateBatch(int count)
    {
        var newIndex = Interlocked.Add(ref _sharedIndex, count);
        var startIndex = newIndex - count;

        if (startIndex >= Capacity) return -1;
        if (newIndex > Capacity) return -1;

        return startIndex;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Reset() => _sharedIndex = _firstIndex;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void BuildMemory(int index, out SnakesSystem system, out Bitboard foodBitboard, out Bitboard hazardsBitboard, out Bitboard collisionsBitboard)
    {
        // Thread-Safe: L'accesso in lettura/scrittura è sicuro fintanto che 'index' è univoco per il thread corrente.
        // Poiché Allocate/AllocateBatch garantiscono indici univoci, non servono lock qui.
        var slotPtr = _basePointer + (nuint)index * _stride;
        
        system = new(slotPtr, in _layout, _snakesCount);

        foodBitboard = new(new(slotPtr + _layout.FoodBitboard.Offset, (int)_layout.FoodBitboard.Length));
        hazardsBitboard = new(new(slotPtr + _layout.HazardsBitboard.Offset, (int)_layout.HazardsBitboard.Length));
        collisionsBitboard = new(new(slotPtr + _layout.CollisionsBitboard.Offset, (int)_layout.CollisionsBitboard.Length));
    }

    public void Dispose()
    {
        if (_disposed || _basePointer is null) return;
        
        NativeMemory.AlignedFree(_basePointer);
        _basePointer = null;
        _disposed = true;
    }
}