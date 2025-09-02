using System.Runtime.InteropServices;
using Thanos.PreWarm.Memory;

namespace Thanos.Memory;

public sealed unsafe class SlotMemoryPool : IDisposable
{
    private readonly byte* _basePointer;
    private readonly int _slotSize;
    private GameContext _context;
    private Luts _luts;

    public SlotMemoryPool(in GameContext context, long maxNodes)
    {
        _context = context;
        _slotSize = context.Layout.WarSlotSize;

        var totalSize = _slotSize * maxNodes;
        _basePointer = (byte*)NativeMemory.AlignedAlloc((nuint)totalSize, 64);
        NativeMemory.Clear(_basePointer, (nuint)totalSize);
    }

    public MemorySlot this[int index]
    {
        get
        {
            var startOffset = (long)index * _slotSize;
            var slotPointer = _basePointer + startOffset;
            var slotSpan = new Span<byte>(slotPointer, _slotSize);

            return new MemorySlot(slotSpan, ref _context, ref _luts);
        }
    }

    public void Dispose() => NativeMemory.Free(_basePointer);

    public void Set(in GameContext context, in Luts luts)
    {
        _context = context;
        _luts = luts;
    }
}