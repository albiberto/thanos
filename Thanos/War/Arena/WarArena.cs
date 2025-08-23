using System.Runtime.InteropServices;
using Thanos.War.Grid;
using Thanos.War.Snake;

namespace Thanos.War.Arena;

[StructLayout(LayoutKind.Sequential)]
public ref struct WarArena
{
    private ref WarArenaHeader _header;
    private readonly WarGrid _grid;
    private readonly WarSnakes _snakes;
    
    public WarArena(ref WarArenaHeader header, WarGrid grid, WarSnakes snakes)
    {
        _header = ref header;
        _grid = grid;
        _snakes = snakes;
    }

    public byte GetLegalMoves(Snake.WarSnake warSnake)
    {
        var head = warSnake.Head;
        
        // Ottiene le 4 posizioni adiacenti dalla LUT. Nessun calcolo, solo lookup.
        var upPos = _movesLut.GetNeighbor(head, Moves.Up);
        var downPos = _movesLut.GetNeighbor(head, Moves.Down);
        var leftPos = _movesLut.GetNeighbor(head, Moves.Left);
        var rightPos = _movesLut.GetNeighbor(head, Moves.Right);

        // Converte il bool 'isNotOccupied' (true/false) in 1/0 senza 'if'.
        var upValid = Unsafe.As<bool, byte>(ref Unsafe.AsRef(!_grid.IsOccupied(upPos)));
        var downValid = Unsafe.As<bool, byte>(ref Unsafe.AsRef(!_grid.IsOccupied(downPos)));
        var leftValid = Unsafe.As<bool, byte>(ref Unsafe.AsRef(!_grid.IsOccupied(leftPos)));
        var rightValid = Unsafe.As<bool, byte>(ref Unsafe.AsRef(!_grid.IsOccupied(rightPos)));

        // Combina i risultati usando la matematica invece degli 'if'.
        return (byte)(
            (upValid * Moves.Up) |
            (downValid * Moves.Down) |
            (leftValid * Moves.Left) |
            (rightValid * Moves.Right)
        );
    }
    
    public float Evaluate()
    {
        if (WarSnakes[0].Dead) return -1.0f;
        return _header.LiveSnakesCount <= 1 ? 1.0f : 0.0f;
    }
}