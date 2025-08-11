using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Thanos.SourceGen;

namespace Thanos.War;

[StructLayout(LayoutKind.Sequential)]
public readonly unsafe struct WarField
{
    public const int TotalBitboards = 3; // Food, Hazard, AllSnakes

    private readonly uint _width;
    
    private readonly ulong* _foodBoard;
    private readonly ulong* _hazardBoard;
    private readonly ulong* _snakesBoard;
    
    public WarField(ulong* memory, uint width, uint bitboardSize)
    {
        _width = width;
        
        _foodBoard = memory;
        _hazardBoard = memory + bitboardSize;
        _snakesBoard = memory + bitboardSize * 2;
        
        var bitBoardsSegments = bitboardSize * 3;
        NativeMemory.Clear(memory, bitBoardsSegments * sizeof(ulong));
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddFood(ReadOnlySpan<Coordinate> foods)
    {
        foreach (ref readonly var food in foods) SetBit(_foodBoard, To1D(food));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddHazard(ReadOnlySpan<Coordinate> hazards)
    {
        foreach (ref readonly var hazard in hazards) SetBit(_hazardBoard, To1D(hazard));
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetSnakeBit(ushort position1D) => SetBit(_snakesBoard, position1D);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void SetBit(ulong* board, ushort position1D)
    {
        // Calcola in quale ulong dell'array si trova la nostra casella
        var ulongIndex = position1D >> 6;        // divisione per 64
        
        // Calcola la posizione del bit all'interno di quell'ulong
        var bitIndex = position1D & 63;          // modulo 64

        // Imposta quel bit a 1 usando una maschera e un'operazione OR
        board[ulongIndex] |= 1UL << bitIndex;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort To1D(Coordinate coord) => (ushort)(coord.Y * _width + coord.X);
}