using System.Runtime.InteropServices;
using Thanos.PreWarm.Memory;
using Thanos.SourceGen;
using Thanos.War;

namespace Thanos.Memory;

public sealed unsafe class SlotMemoryPool : IDisposable
{
    private readonly void* _basePointer;
    private readonly uint _maxSlots;
    private int _area;

    private MemoryLayout _layout;
    private LookupPointers _lookupPointers;
    private Dictionary<string, int> _map;

    public SlotMemoryPool(uint maxSlots, in MemoryLayout layout, LookupPointers? lutPointers = null, Dictionary<string, int>? map = null, int area = 0)
    {
        _maxSlots = maxSlots;

        _layout = layout;
        _lookupPointers = lutPointers ?? default;
        _area = area;
        _map = map ?? [];

        var totalSize = layout.SlotSize * maxSlots;
        _basePointer = NativeMemory.AlignedAlloc((nuint)totalSize, 64);

        Console.WriteLine($"[NodeMemoryPool] Allocated {(double)totalSize / (1024 * 1024 * 1024):F3} GB for {_layout.SlotSize}-byte nodes, max nodes: {_maxSlots}");
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
        if (index >= _maxSlots) throw new IndexOutOfRangeException("Accesso illegale allo SlotMemoryPool. Richiesto indice " + index + ", ma la capacità massima è " + _maxSlots + ".");

        var pointer = (byte*)_basePointer + index * _layout.SlotSize;
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

    public void Set(int area, LookupPointers lookupPointers, Dictionary<string, int> map, in MemoryLayout layout)
    {
        _area = area;
        _lookupPointers = lookupPointers;
        _map = map;
        _layout = layout;
    }
}