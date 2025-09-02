using System.Runtime.InteropServices;
using Thanos.PreWarm.Memory;

namespace Thanos.Memory;

public sealed unsafe class SlotMemoryPool : IDisposable
{
    // Memoria per l'albero MCTS
    private byte* TreeMemoryBasePointer { get; }

    // Memoria separata per le simulazioni
    private byte* SandboxMemoryBasePointer { get; }
    
    private readonly int _slotSize;
    private GameContext _context;
    private Luts _luts;

    public SlotMemoryPool(in GameContext context, long maxTreeNodes, int sandboxCount)
    {
        _context = context;
        _slotSize = context.Layout.WarSlotSize;

        // Alloca memoria per l'albero
        var treeTotalSize = _slotSize * maxTreeNodes;
        TreeMemoryBasePointer = (byte*)NativeMemory.AlignedAlloc((nuint)treeTotalSize, 64);
        NativeMemory.Clear(TreeMemoryBasePointer, (nuint)treeTotalSize);

        // Alloca memoria separata per le sandbox
        long sandboxTotalSize = _slotSize * sandboxCount;
        SandboxMemoryBasePointer = (byte*)NativeMemory.AlignedAlloc((nuint)sandboxTotalSize, 64);
        NativeMemory.Clear(SandboxMemoryBasePointer, (nuint)sandboxTotalSize);
    }
    
    // L'indexer ora accede solo alla memoria dell'albero
    public MemorySlot this[int index]
    {
        get
        {
            var startOffset = (long)index * _slotSize;
            var slotPointer = TreeMemoryBasePointer + startOffset;
            var slotSpan = new Span<byte>(slotPointer, _slotSize);
            return new MemorySlot(slotSpan, ref _context, ref _luts);
        }
    }
    
    // GetSandBox ora accede solo alla memoria della sandbox
    public MemorySlot GetSandBox(int sandboxId = 0)
    {
        var startOffset = (long)sandboxId * _slotSize;
        var slotPointer = SandboxMemoryBasePointer + startOffset;
        var slotSpan = new Span<byte>(slotPointer, _slotSize);
        return new MemorySlot(slotSpan, ref _context, ref _luts);
    }

    public void Dispose()
    {
        NativeMemory.Free(TreeMemoryBasePointer);
        NativeMemory.Free(SandboxMemoryBasePointer);
    }

    public void Set(in GameContext context, in Luts luts)
    {
        _context = context;
        _luts = luts;
    }
}