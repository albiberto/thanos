using System.Runtime.InteropServices;
using Thanos.Memory.Pools;
using Thanos.War;

public sealed unsafe class SlotMemoryPool : IDisposable
{
    private readonly MemoryLayout _layout;
    private readonly int _maxSlots;

    private readonly void* _basePointer;
    
    public SlotMemoryPool(in MemoryLayout layout, int maxSlots)
    {
        _layout = layout;
        _maxSlots = maxSlots;

        var totalSize = layout.SlotSize * maxSlots;
        _basePointer = NativeMemory.AlignedAlloc((nuint)totalSize, 64);
    }
    
    public WarArena this[int index]
    {
        get
        {
            if (index >= _maxSlots) throw new IndexOutOfRangeException();
            
            var offset = index * _layout.SlotSize;

            var slotPointer = (byte*)_basePointer + offset;
            var slotSpan = new Span<byte>(slotPointer, _layout.SlotSize);

            return new WarArena(slotSpan, _layout, 8, null); // Esempio
        }
    }

    public void Dispose() => NativeMemory.AlignedFree(_basePointer);
}