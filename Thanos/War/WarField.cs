using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Thanos.SourceGen;

namespace Thanos.War;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WarField
{
    public const int TotalBitboards = 3;
    
    public uint Width, Height, Area, _bitboardSegments;

    private ulong* _foodBitboard;
    private ulong* _hazardBitboard;
    private ulong* _snakesBitboard;

    // Le proprietà sono il punto di accesso corretto
    private Bitboard Food => new(_foodBitboard, _bitboardSegments);
    private Bitboard Hazard => new(_hazardBitboard, _bitboardSegments);
    public Bitboard Snakes => new(_snakesBitboard, _bitboardSegments);

    public static void PlacementNew(WarField* fieldPtr, in WarContext context, ReadOnlySpan<Coordinate> food, ReadOnlySpan<Coordinate> hazards, uint bitboardSegments, ulong* foodBitboardPtr, ulong* hazardBitboardPtr, ulong* snakesBitboardPtr)
    {
        fieldPtr->Width = context.Width;
        fieldPtr->Height = context.Height;
        fieldPtr->Area = context.Area;
        fieldPtr->_bitboardSegments = bitboardSegments;
        fieldPtr->_foodBitboard = foodBitboardPtr;
        fieldPtr->_hazardBitboard = hazardBitboardPtr;
        fieldPtr->_snakesBitboard = snakesBitboardPtr;

        fieldPtr->Food.ClearAll();
        fieldPtr->Hazard.ClearAll();
        fieldPtr->Snakes.ClearAll();

        foreach (ref readonly var coordinate in food) { fieldPtr->Food.Set(To1D(in coordinate, fieldPtr->Width)); }
        foreach (ref readonly var coordinate in hazards) { fieldPtr->Hazard.Set(To1D(in coordinate, fieldPtr->Width)); }
    }

    // --- METODI DI SCRITTURA (ORA CORRETTI E COERENTI) ---

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetSnakeBit(ushort position1D)
    {
        // CORRETTO: Delega l'operazione alla proprietà Snakes.
        Snakes.Set(position1D);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UpdateSnakePosition(ushort oldTail, ushort newHead, bool hasEaten)
    {
        // CORRETTO: Usa le proprietà per un codice più pulito e coerente.
        Snakes.Set(newHead);

        if (!hasEaten)
        {
            Snakes.Clear(oldTail);
        }
        else
        {
            Food.Clear(newHead);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RemoveSnake(ReadOnlySpan<ushort> body)
    {
        // CORRETTO: Usa la proprietà Snakes nel ciclo.
        foreach (var position in body)
        {
            Snakes.Clear(position);
        }
    }

    // --- METODI DI LETTURA "HOT PATH" (invariati per massime performance) ---

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool IsOccupied(ushort position1D)
    {
        if (position1D == ushort.MaxValue) return true;
        var ulongIndex = position1D >> 6;
        var bitMask = 1UL << (position1D & 63);
        return ((_hazardBitboard[ulongIndex] | _snakesBitboard[ulongIndex]) & bitMask) != 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool IsFood(ushort position1D)
    {
        if (position1D >= Area) return false;
        var index = position1D >> 6;
        var mask = 1UL << (position1D & 63);
        return (_foodBitboard[index] & mask) != 0;
    }
    
    // --- METODI HELPER ---

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ushort To1D(in Coordinate coord) => (ushort)(coord.Y * Width + coord.X);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ushort To1D(in Coordinate coord, uint width) => (ushort)(coord.Y * width + coord.X);
}