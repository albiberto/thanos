using System.Runtime.InteropServices;
using Thanos.War.Grid;
using Thanos.War.Snake;
using Thanos.War.Snake.Memory;

namespace Thanos.War.Arena;

[StructLayout(LayoutKind.Sequential)]
public ref struct WarArena(WarGrid grid, WarSnakesMemoryView snakesMemoryView)
{
    private readonly WarGrid _grid = grid;
    private readonly WarSnakesMemoryView _snakesMemoryView = snakesMemoryView;

    // public float Evaluate()
    // {
    //     if (WarSnakes[0].Dead) return -1.0f;
    //     return _header.LiveSnakesCount <= 1 ? 1.0f : 0.0f;
    // }
}