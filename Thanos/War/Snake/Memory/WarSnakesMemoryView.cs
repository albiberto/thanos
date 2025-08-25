using System.Runtime.CompilerServices;

namespace Thanos.War.Snake.Memory;

public readonly ref struct WarSnakesMemoryView(Span<byte> memory, ref WarSnakeMemoryLayout layout)
{
    private readonly Span<byte> _memory = memory;
    private readonly ref WarSnakeMemoryLayout _memoryLayout = ref layout;
    
    public WarSnake this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(new WarSnakeMemoryView(_memory, in _memoryLayout, index));
    }
    
    public WarSnake Me
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(new WarSnakeMemoryView(_memory, in _memoryLayout, 0));
    }
}