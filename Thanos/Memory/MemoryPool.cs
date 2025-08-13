using Thanos.War;

namespace Thanos.Memory;

public sealed unsafe class MemoryPool(byte* poolStartPtr)
{
    private long _currentOffset;
    
    private MemoryLayout _layout;
    private WarContext _context;
    
    public bool TryGetNext(out MemorySlot builder)
    {
        if ((ulong)(_currentOffset + _layout.Sizes.Slot) >= _layout.Sizes.Pool)
        {
            builder = default;
            return false;
        }
    
        var currentSlotPtr = poolStartPtr + _currentOffset;

        builder = new MemorySlot(currentSlotPtr, _context, _layout);
    
        _currentOffset += _layout.Sizes.Slot;
        return true;
    }

    public void Reset() => _currentOffset = 0;

    public void Reset(in WarContext context, in MemoryLayout layout)
    {
        Reset();

        _context = context;
        _layout = layout;
    }
}