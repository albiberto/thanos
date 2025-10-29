using System.Runtime.InteropServices;
using Thanos.War;
using Thanos.War.Structures;

namespace Thanos.Memory;

public sealed unsafe class SlotMemoryPool : IDisposable
{
    private readonly void* _basePointer;

    private readonly LookupsMemoryPool _lookupsMemoryPool;
    private readonly SlotMemoryLayout _layout;
    private readonly int _slotSize;

    private Dictionary<string, int>? _map;

    public SlotMemoryPool(uint maxSlots, LookupsMemoryPool lookupsMemoryPool, in SlotMemoryLayout layout)
    {
        _lookupsMemoryPool = lookupsMemoryPool;
        _layout = layout;
        _slotSize = _layout.SlotSize;

        var memorySize = (nuint)_slotSize * maxSlots * Constants.MaxSnakesCount;
        _basePointer = NativeMemory.AlignedAlloc(memorySize, 64);

        Console.WriteLine($"[SlotMemoryPool] Allocated {(double)memorySize / (1024 * 1024 * 1024):F3} GB for {_slotSize}-byte nodes, max nodes: {maxSlots}");
    }

    public Arena GetArena(int index)
    {
        var slotMemory = GetSlotSpan(index);
        var system = GetSnakesSystem(index);
        BuildBitboards(slotMemory, out var food, out var hazards, out var snakes);

        return new Arena(
            system,
            food,
            hazards,
            snakes,
            _map ?? [],
            _lookupsMemoryPool.NeighborsGrid,
            _lookupsMemoryPool.ConversionsMap);
    }

    public Heuristics GetHeuristics(int index)
    {
        var slotMemory = GetSlotSpan(index);
        var system = GetSnakesSystem(index);
        BuildBitboards(slotMemory, out var food, out var hazards, out var snakes);

        return new Heuristics(
            system,
            food,
            hazards,
            snakes,
            _lookupsMemoryPool.NeighborsGrid,
            _lookupsMemoryPool.ConversionsMap,
            _lookupsMemoryPool.PositionalScores);
    }

    public void Set(Dictionary<string, int> map) => _map = map;

    private Span<byte> GetSlotSpan(int index)
    {
        var pointer = (byte*)_basePointer + index * _slotSize;
        return new Span<byte>(pointer, _slotSize);
    }

    private SnakesSystem GetSnakesSystem(int index)
    {
        var memory = GetSlotSpan(index);
        return new SnakesSystem(memory, in _layout, _map?.Count ?? 0);
    }

    private void BuildBitboards(Span<byte> slotMemory, out Bitboard food, out Bitboard hazards, out Bitboard snakes)
    {
        food = new Bitboard(slotMemory.Slice(_layout.FoodBitboardOffset, _layout.BitboardSize));
        hazards = new Bitboard(slotMemory.Slice(_layout.HazardsBitboardOffset, _layout.BitboardSize));
        snakes = new Bitboard(slotMemory.Slice(_layout.SnakesBitboardOffset, _layout.BitboardSize));
    }

    public void Dispose() => NativeMemory.AlignedFree(_basePointer);
}