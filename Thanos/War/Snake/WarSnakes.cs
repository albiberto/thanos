using System.Runtime.CompilerServices;
using Thanos.War.Snake.Memory;

namespace Thanos.War.Snake;

public readonly ref struct WarSnakes(Span<byte> memery, in WarSnakeMemoryLayout layout)
{
    private readonly Span<byte> _memery = memery;
    private readonly ref WarSnakeMemoryLayout _memoryLayout = ref layout;
    
    public WarSnake this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(new WarSnakeMemoryView(_memery, in _memoryLayout, index));
    }
}