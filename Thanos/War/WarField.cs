using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Thanos.SourceGen;

namespace Thanos.War;

[StructLayout(LayoutKind.Sequential)]
public readonly unsafe struct WarField
{
    public const int TotalBitboards = 3; // Food, Hazard, AllSnakes

    public readonly uint Width;
    public readonly uint Area;
    private readonly uint _ulongsPerBitboard;
    private readonly ulong* _memory;

    // --- Mappatura dei Bitboard ---
    private ulong* FoodBoard => _memory;
    private ulong* HazardBoard => _memory + _ulongsPerBitboard;
    private ulong* AllSnakesBoard => _memory + _ulongsPerBitboard * 2;
    
    // CORREZIONE: Il costruttore calcola da solo il numero di ulong necessari
    public WarField(ulong* memory, uint width, uint area)
    {
        Width = width;
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
}