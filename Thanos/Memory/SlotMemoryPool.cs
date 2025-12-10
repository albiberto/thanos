using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Thanos.Abstract;
using Thanos.War;
using Thanos.War.Structures;

namespace Thanos.Memory;

public sealed unsafe class SlotMemoryPool : ISlotMemoryPool
{
    private readonly byte* _basePointer;

    private readonly int _firstIndex;
    private readonly int _snakesCount;
    private readonly nuint _stride; 
    private readonly SlotMemoryLayout _layout;
    private readonly ILookupsMemoryPool _lookupsMemoryPool;
    
    public uint Capacity { get; }
    public int Index { get; private set; }

    // Aggiunto activeSnakeCount al costruttore
    public SlotMemoryPool(uint capacity, int firstIndex, int snakesCount, ILookupsMemoryPool lookupsMemoryPool, in SlotMemoryLayout layout)
    {
        _firstIndex = firstIndex;
        _snakesCount = snakesCount;
        
        _layout = layout;
        _stride = layout.SlotStride.Next;
        
        _lookupsMemoryPool = lookupsMemoryPool;
        
        Capacity = capacity;
        Index = _firstIndex;
        
        var totalSize = _stride * (nuint)capacity;
        _basePointer = (byte*)NativeMemory.AlignedAlloc(totalSize, Constants.CacheLine);
        NativeMemory.Clear(_basePointer, totalSize);
    }

    // Il metodo Configure è stato rimosso. Il pool è immutabile nella sua configurazione.

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Arena GetArena(int index)
    {
        BuildMemory(index, out var system, out var foodBitboard, out var hazardsBitboard, out var collisionsBitboard);
        return new Arena(system, foodBitboard, hazardsBitboard, collisionsBitboard, _lookupsMemoryPool.NeighborsMatrix, _lookupsMemoryPool.CoordinatesMatrix);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Heuristics GetHeuristics(int index)
    {
        BuildMemory(index, out var system, out var foodBitboard, out var hazardsBitboard, out var collisionsBitboard);
        return new Heuristics(system, foodBitboard, hazardsBitboard, collisionsBitboard, _lookupsMemoryPool.NeighborsMatrix, _lookupsMemoryPool.CoordinatesMatrix);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Allocate()
    {
        if (Index >= Capacity) return -1;
        return Index++;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Reset() => Index = _firstIndex;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void BuildMemory(int index, out SnakesSystem system, out Bitboard foodBitboard, out Bitboard hazardsBitboard, out Bitboard collisionsBitboard)
    {
        var slotPtr = _basePointer + (nuint)index * _stride;
        
        system = new SnakesSystem(new Span<byte>(slotPtr, (int)_layout.SlotStride.Length), in _layout, _snakesCount);

        foodBitboard = new Bitboard(new Span<byte>(slotPtr + _layout.FoodBitboard.Offset, (int)_layout.FoodBitboard.Length));
        hazardsBitboard = new Bitboard(new Span<byte>(slotPtr + _layout.HazardsBitboard.Offset, (int)_layout.HazardsBitboard.Length));
        collisionsBitboard = new Bitboard(new Span<byte>(slotPtr + _layout.CollisionsBitboard.Offset, (int)_layout.CollisionsBitboard.Length));
    }

    public void Dispose() => NativeMemory.AlignedFree(_basePointer);
}