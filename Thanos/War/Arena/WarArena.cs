using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Thanos.MCST;
using Thanos.Memory;
using Thanos.War.Grid;
using Thanos.War.Snake;

namespace Thanos.War.Arena;

[StructLayout(LayoutKind.Sequential)]
public ref struct WarArena
{
    private ref WarArenaHeader _header;
    private readonly WarGrid _grid;
    private readonly WarSnakes _snakes;
    
    // NOTA: I parametri sono stati corretti per riflettere la gestione della memoria.
    public WarArena(ref WarArenaHeader header, WarGrid grid, Span<byte> snakesMemory, in MemoryLayout layout)
    {
        _header = ref header;
        _grid = grid;
        _snakes = new WarSnakes(in layout, snakesMemory);
    }
}
    

    /// <summary>
    /// Returns the legal moves for a snake.
    /// This version is almost branchless, relying on a pre-computed lookup table.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
        if (Snakes[0].Dead) return -1.0f;
        return _header.LiveSnakesCount <= 1 ? 1.0f : 0.0f;
    }
}