using System.Runtime.InteropServices;
using Thanos.War;

namespace Thanos.Memory;

public sealed unsafe class SlotMemoryPool : IDisposable
{
    private readonly uint _maxSlots;
 
    private MemoryLayout _layout;
    private int _snakesCount;
    private ushort[] _neighbors;

    private readonly void* _basePointer;
    
    public SlotMemoryPool(uint maxSlots, in MemoryLayout layout, ushort[] neighbors, int snakesCount)
    {
        _maxSlots = maxSlots;
        
        _layout = layout;
        _neighbors = neighbors;
        _snakesCount = snakesCount;

        var totalSize = layout.SlotSize * maxSlots;
        _basePointer = NativeMemory.AlignedAlloc((nuint)totalSize, 64);
    }
    
    public Arena this[uint index]
    {
        get
        {
            if (index >= _maxSlots) throw new IndexOutOfRangeException("Accesso illegale allo SlotMemoryPool. Richiesto indice " + index + ", ma la capacità massima è " + _maxSlots + ".");
                
            var pointer = (byte*)_basePointer + index * _layout.SlotSize;
            var memory = new Span<byte>(pointer, _layout.SlotSize);
            
            var foodBitboardMemory = memory.Slice(_layout.FoodBitboardOffset, _layout.BitboardSize);
            var hazardsBitboardMemory = memory.Slice(_layout.HazardsBitboardOffset, _layout.BitboardSize);
            
            var foodBitboard = new Bitboard(foodBitboardMemory);
            var hazardsBitboard = new Bitboard(hazardsBitboardMemory);
            var snakesSystem = new SnakesSystem(memory, _layout, _snakesCount);
                
            return new Arena(snakesSystem, foodBitboard, hazardsBitboard, _neighbors);
        }
    }

    public void Set(in MemoryLayout layout, ushort[] neighbors, int snakesCount)
    {
        _layout = layout;
        _neighbors = neighbors;
        _snakesCount = snakesCount;
    }

    public void Dispose() => NativeMemory.AlignedFree(_basePointer);
}