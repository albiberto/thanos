using System.Runtime.InteropServices;
using Thanos.War.Grid;
using Thanos.War.Snake;

namespace Thanos.War.Arena;

[StructLayout(LayoutKind.Sequential)]
public ref struct WarArena
{
    private readonly ref WarArenaHeader _header;
    private readonly ref Geography _movesLut;
    private readonly WarGrid _grid;
    private readonly WarSnakes _snakes;

    public WarArena(ref WarArenaHeader header, WarGrid grid, WarSnakes snakes)
    {
        _header = ref header;
        
        _grid = grid;
        _snakes = snakes;
    }
    
    // public float Evaluate()
    // {
    //     if (WarSnakes[0].Dead) return -1.0f;
    //     return _header.LiveSnakesCount <= 1 ? 1.0f : 0.0f;
    // }
}