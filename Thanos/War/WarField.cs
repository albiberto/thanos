using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Thanos.MCST;
using Thanos.SourceGen;

namespace Thanos.War;

[StructLayout(LayoutKind.Sequential)]
public readonly unsafe struct WarField
{
    public const int TotalBitboards = 3; // Food, Hazard, AllSnakes

    public readonly uint Width;
    private readonly uint _height;
    public readonly uint Area;
    private readonly uint _ulongsPerBitboard;
    private readonly ulong* _memory;

    // --- Mappatura dei Bitboard ---
    private ulong* FoodBoard => _memory;
    private ulong* HazardBoard => _memory + _ulongsPerBitboard;
    private ulong* AllSnakesBoard => _memory + _ulongsPerBitboard * 2;
    
    // CORREZIONE: Il costruttore calcola da solo il numero di ulong necessari
    public WarField(ulong* memory, uint width, uint height, uint area)
    {
        Width = width;
        _height = height;
        Area = area;
        _memory = memory;
        _ulongsPerBitboard = (Area + 63) / 64;

        var totalUlongs = _ulongsPerBitboard * TotalBitboards;
        NativeMemory.Clear(_memory, (uint)(totalUlongs * sizeof(ulong)));
    }

    public void InitializeStaticBoards(in Board board)
    {
        foreach (ref readonly var foodCoord in board.Food.AsSpan())
            SetBit(FoodBoard, To1D(in foodCoord));

        foreach (ref readonly var hazardCoord in board.Hazards.AsSpan())
            SetBit(HazardBoard, To1D(in hazardCoord));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetSnakeBit(ushort position1D) => SetBit(AllSnakesBoard, position1D);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SetBit(ulong* board, ushort position1D) => board[position1D >> 6] |= 1UL << (position1D & 63);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort To1D(in Coordinate coord) => (ushort)(coord.Y * Width + coord.X);
    
    /// <summary>
    /// Calcola la posizione 1D di una casella adiacente a una data posizione.
    /// </summary>
    /// <returns>La coordinata 1D del vicino, o ushort.MaxValue se la mossa porta fuori dalla scacchiera.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort GetNeighbor(ushort position1D, MoveDirection direction)
    {
        // Converte temporaneamente 1D in 2D per un calcolo sicuro dei bordi
        var x = position1D % Width;
        var y = position1D / Width;

        switch (direction)
        {
            case MoveDirection.Up:    y--; break;
            case MoveDirection.Down:  y++; break;
            case MoveDirection.Left:  x--; break;
            case MoveDirection.Right: x++; break;
        }

        // Controlla se siamo finiti fuori dalla scacchiera (collisione con un muro)
        if (x < 0 || x >= Width || y < 0 || y >= _height)
        {
            return ushort.MaxValue; // Un valore sentinella per indicare "fuori dai limiti"
        }

        // Riconverte le coordinate 2D valide in un indice 1D
        return (ushort)(y * Width + x);
    }

    /// <summary>
    /// Controlla se una casella è occupata da un ostacolo (muro, pericolo o un altro serpente).
    /// Questa è un'operazione O(1) ultra-veloce grazie ai bitboard.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsOccupied(ushort position1D)
    {
        // Se la posizione è il nostro valore sentinella, è un muro.
        if (position1D == ushort.MaxValue) return true;

        // Calcola l'indice e la maschera per interrogare i bitboard
        var ulongIndex = position1D >> 6;
        var bitMask = 1UL << (position1D & 63);

        // Una casella è occupata se il bit corrispondente è acceso
        // nel bitboard dei pericoli OPPURE nel bitboard di tutti i serpenti.
        return ((HazardBoard[ulongIndex] | AllSnakesBoard[ulongIndex]) & bitMask) != 0;
    }
}