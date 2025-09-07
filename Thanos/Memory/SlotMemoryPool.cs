using System.Runtime.InteropServices;
using Thanos.War;

namespace Thanos.Memory;

public sealed unsafe class SlotMemoryPool : IDisposable
{
    private readonly uint _maxSlots;
 
    private MemoryLayout _layout;
    private ushort[] _neighbors;
    private int _area;
    private Dictionary<Guid, int> _map;

    private readonly void* _basePointer;
    
    public SlotMemoryPool(uint maxSlots, in MemoryLayout layout, ushort[] neighbors, int area, Dictionary<Guid, int> map)
    {
        _maxSlots = maxSlots;
        
        _layout = layout;
        _neighbors = neighbors;
        _area = area;
        _map = map;

        var totalSize = layout.SlotSize * maxSlots;
        _basePointer = NativeMemory.AlignedAlloc((nuint)totalSize, 64);
        
        Console.WriteLine($"[NodeMemoryPool] Allocated {(double)totalSize / (1024 * 1024 * 1024):F3} GB for {_layout.SlotSize}-byte nodes, max nodes: {_maxSlots}");
    }
    
    public Arena this[int index]
    {
        get
        {
            if (index >= _maxSlots) throw new IndexOutOfRangeException("Accesso illegale allo SlotMemoryPool. Richiesto indice " + index + ", ma la capacità massima è " + _maxSlots + ".");
                
            var pointer = (byte*)_basePointer + index * _layout.SlotSize;
            var memory = new Span<byte>(pointer, _layout.SlotSize);
            
            var foodBitboardMemory = memory.Slice(_layout.FoodBitboardOffset, _layout.BitboardSize);
            var hazardsBitboardMemory = memory.Slice(_layout.HazardsBitboardOffset, _layout.BitboardSize);
            var snakesBitboardMemory = memory.Slice(_layout.SnakesBitboardOffset, _layout.BitboardSize);

            var snakesSystem = new SnakesSystem(memory, _layout, _map.Count);
                
            return new Arena(snakesSystem, foodBitboardMemory, hazardsBitboardMemory, snakesBitboardMemory, _neighbors, _map, _area);
        }
    }

    public void Set(in MemoryLayout layout, ushort[] neighbors, int area, Dictionary<Guid, int> map)
    {
        _layout = layout;
        _neighbors = neighbors;
        _map = map;
        _area = area;
    }

    public void Dispose() => NativeMemory.AlignedFree(_basePointer);
}