using System.Runtime.InteropServices;
using Thanos.PreWarm.Memory;
using Thanos.SourceGen;
using Thanos.War;
using Thanos.War.Structures;

namespace Thanos.Memory;

public sealed unsafe class SlotMemoryPool : IDisposable
{
    private readonly void* _basePointer;

    private SlotMemoryLayout _layout;
    private LookupPointers _lookupPointers;
    private Dictionary<string, int> _map;

    private int _slotSize;

    public SlotMemoryPool(uint maxSlots, in SlotMemoryLayout layout, Dictionary<string, int>? map = null, LookupPointers? lutPointers = null)
    {
        _layout = layout;
        _lookupPointers = lutPointers ?? default;
        _map = map ?? [];

        _slotSize = layout.SnakeStride * map?.Count ?? Constants.MaxSnakesCount + Constants.GlobalBitboardsCount;
        var memorySize = _slotSize * maxSlots;
        _basePointer = NativeMemory.AlignedAlloc((nuint)memorySize, 64);

        Console.WriteLine($"[NodeMemoryPool] Allocated {(double)memorySize / (1024 * 1024 * 1024):F3} GB for {slotSize}-byte nodes, max nodes: {maxSlots}");
    }

    public void Dispose() => NativeMemory.AlignedFree(_basePointer);

    public Arena GetArena(int index)
    {
        BuildViews(index, out var system, out var food, out var hazards, out var snakes, out var neighbors);
    
        var conversionsMapMemory = new ReadOnlySpan<Coordinate>(_lookupPointers.ConversionsMapPtr, _lookupPointers.ConversionsMapLength);
   
        return new Arena(system, food, hazards, snakes, neighbors, _map, conversionsMapMemory);
    }

    public Heuristics GetHeuristics(int index)
    {
        BuildViews(index, out var system, out var food, out var hazards, out var snakes, out var neighbors);

        var positionalScoresMemory = new ReadOnlySpan<float>(_lookupPointers.PositionalScoresPtr, _lookupPointers.PositionalScoresLength);
        var conversionsMapMemory = new ReadOnlySpan<Coordinate>(_lookupPointers.ConversionsMapPtr, _lookupPointers.ConversionsMapLength);

        return new Heuristics(system, food, hazards, snakes, neighbors, conversionsMapMemory, positionalScoresMemory);
    }

    private void BuildViews(int index, out SnakesSystem system, out Bitboard food, out Bitboard hazards, out Bitboard snakes, out NeighborsGrid neighbors)
    {
        var pointer = (byte*)_basePointer + index * _slotSize;
        var memory = new Span<byte>(pointer, _layout.SlotSize);

        system = new SnakesSystem(memory, _layout, _map.Count);

        var foodBitboardMemory = memory.Slice(_layout.FoodBitboardOffset, _layout.BitboardSize);
        var hazardsBitboardMemory = memory.Slice(_layout.HazardsBitboardOffset, _layout.BitboardSize);
        var snakesBitboardMemory = memory.Slice(_layout.SnakesBitboardOffset, _layout.BitboardSize);
        var neighborsMemory = new ReadOnlySpan<ushort>(_lookupPointers.NeighborsPtr, _lookupPointers.NeighborsLength);

        food = new Bitboard(foodBitboardMemory);
        hazards = new Bitboard(hazardsBitboardMemory);
        snakes = new Bitboard(snakesBitboardMemory);
        neighbors = new NeighborsGrid(_area, neighborsMemory);
    }

    public void Set(LookupPointers lookupPointers, Dictionary<string, int> map, in SlotMemoryLayout layout)
    {
        _lookupPointers = lookupPointers;
        _map = map;
        _layout = layout;
    }
}