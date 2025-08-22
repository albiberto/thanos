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
    // --- CAMPI PRIVATI ---
    private ref WarArenaHeader _header;
    private ref MemoryLayout _layout;
    private WarGrid _grid;
    private readonly Span<byte> _snakesMemory;

    /// <summary>
    ///     Crea una nuova vista WarArena per uno stato di gioco esistente.
    /// </summary>
    public WarArena(in MemoryLayout layout, ref WarArenaHeader header, WarGrid grid, Span<byte> snakesMemory)
    {
        _layout = layout;
        _header = ref header;
        _grid = grid;
        _snakesMemory = snakesMemory;
    }
    
    public WarSnakes Snakes => new(in _layout, _snakesMemory);
    
    public readonly long GetStateHash => _header.Hash;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte GetLegalMoves(WarSnake snake)
    {
        var head = snake.Head;
        var width = _grid.Width;
        var area = _grid.Area;
        
        var legalMoveSet = Moves.None;

        // --- Calcola e Controlla SU ---
        var upPos = head < width ? ushort.MaxValue : (ushort)(head - width);
        if (!_grid.IsOccupied(upPos)) legalMoveSet |= Moves.Up;

        // --- Calcola e Controlla GIÙ ---
        var downPos = head >= area - width ? ushort.MaxValue : (ushort)(head + width);
        if (!_grid.IsOccupied(downPos)) legalMoveSet |= Moves.Down;

        // --- Calcola e Controlla SINISTRA ---
        var leftPos = head % width == 0 ? ushort.MaxValue : (ushort)(head - 1);
        if (!_grid.IsOccupied(leftPos)) legalMoveSet |= Moves.Left;

        // --- Calcola e Controlla DESTRA ---
        var rightPos = (head + 1) % width == 0 ? ushort.MaxValue : (ushort)(head + 1);
        if (!_grid.IsOccupied(rightPos)) legalMoveSet |= Moves.Right;

        return legalMoveSet;
    }

    /// <summary>
    ///     Valuta lo stato finale del gioco dal punto di vista del nostro serpente (indice 0).
    /// </summary>
    /// <returns>1.0 per vittoria, -1.0 per sconfitta, 0.0 se il gioco continua.</returns>
    public float Evaluate()
    {
        if (Snakes[0].Dead) return -1.0f;
        return _header.LiveSnakesCount <= 1 ? 1.0f : 0.0f;
    }
}