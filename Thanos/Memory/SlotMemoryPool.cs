using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Thanos.Abstract;
using Thanos.Common;
using Thanos.War;
using Thanos.War.Structures;

namespace Thanos.Memory;

public sealed unsafe class SlotMemoryPool : ISlotMemoryPool
{
    private readonly byte* _basePointer;
    private readonly LookupsMemoryPool _lookupsMemoryPool;
    private readonly SlotMemoryLayout _layout;
    private readonly int _slotSize;

    public int Capacity { get; }
    public int Count { get; private set; }

    private int _activeSnakeCount;

    public SlotMemoryPool(uint maxSlots, LookupsMemoryPool lookupsMemoryPool, in SlotMemoryLayout layout)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxSlots);

        Capacity = (int)maxSlots;
        Count = 0;

        _lookupsMemoryPool = lookupsMemoryPool;
        _layout = layout;
        _slotSize = _layout.SlotSize;

        var memorySize = (nuint)((long)_slotSize * Capacity);
        
        // Allineamento totale del blocco di memoria
        var alignedTotalSize = (nuint)((int)memorySize).AlignUp64();
        
        _basePointer = (byte*)NativeMemory.AlignedAlloc(alignedTotalSize, Constants.CacheLine);
        
        NativeMemory.Clear(_basePointer, alignedTotalSize);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Configure(int snakeCount)
    {
        _activeSnakeCount = snakeCount;
        Reset();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Allocate()
    {
        if (Count >= Capacity) return -1;
        return Count++;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Reset() => Count = 0;

    public Arena GetArena(int index)
    {
        var slotSpan = GetSlotSpan(index);
        
        var system = new SnakesSystem(slotSpan, in _layout, _activeSnakeCount);
        BuildBitboards(slotSpan, out var food, out var hazards, out var snakes);

        return new Arena(
            system,
            food,
            hazards,
            snakes,
            _lookupsMemoryPool.NeighborsMatrix,
            _lookupsMemoryPool.CoordinatesMatrix);
    }

    public Heuristics GetHeuristics(int index)
    {
        var slotSpan = GetSlotSpan(index);
        var system = new SnakesSystem(slotSpan, in _layout, _activeSnakeCount);
        BuildBitboards(slotSpan, out var food, out var hazards, out var snakes);

        return new Heuristics(
            system,
            food,
            hazards,
            snakes,
            _lookupsMemoryPool.NeighborsMatrix,
            _lookupsMemoryPool.CoordinatesMatrix);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Span<byte> GetSlotSpan(int index)
    {
        return new Span<byte>(_basePointer + ((long)index * _slotSize), _slotSize);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void BuildBitboards(Span<byte> slotMemory, out Bitboard food, out Bitboard hazards, out Bitboard snakes)
    {
        food = new Bitboard(slotMemory.Slice(_layout.FoodBitboard.Offset, _layout.FoodBitboard.Length));
        hazards = new Bitboard(slotMemory.Slice(_layout.HazardsBitboard.Offset, _layout.HazardsBitboard.Length));
        snakes = new Bitboard(slotMemory.Slice(_layout.SnakesBitboard.Offset, _layout.SnakesBitboard.Length));
    }

    public void Dispose() => NativeMemory.AlignedFree(_basePointer);
}