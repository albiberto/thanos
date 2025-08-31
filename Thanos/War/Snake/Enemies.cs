using System.Runtime.CompilerServices;
using Thanos.War.Snake.Memory;

namespace Thanos.War.Snake;

public readonly ref struct Enemies(Span<byte> memory, ref WarSnakeMemoryLayout layout, int count)
{
    public readonly int Count = count;
    
    private readonly Span<byte> _memory = memory;
    private readonly ref WarSnakeMemoryLayout _memoryLayout = ref layout;
    
    public WarSnake this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(new WarSnakeMemoryView(_memory, in _memoryLayout, index));
    }
}