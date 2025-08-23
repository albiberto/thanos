using System.Runtime.CompilerServices;

namespace Thanos.War.Snake;

public readonly ref struct Snakes(in SnakeLayout layout, Span<byte> snakesMemory)
{
    private readonly Span<byte> _snakesMemory = snakesMemory;
    private readonly ref SnakeLayout _layout = ref layout;
    
    public WarSnake this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(new WarSnakeMemoryView(_snakesMemory, in _layout, index));
    }
}

