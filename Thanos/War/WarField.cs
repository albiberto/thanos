using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Thanos.MCST; // Assuming these are your project's using statements
using Thanos.SourceGen;

namespace Thanos.War;

[StructLayout(LayoutKind.Sequential)]
public ref struct WarField
{
    public const int TotalBitboards = 3;
    
    public uint Width { get; }
    public uint Height { get; }
    public uint Area { get; }

    private readonly Span<ulong> _allBitboards;
    private readonly int _segmentLength;

    private readonly Bitboard Food => new(_allBitboards[.._segmentLength]);
    private readonly Bitboard Hazard => new(_allBitboards[_segmentLength .. (_segmentLength * 2)]);
    private readonly Bitboard Snakes => new(_allBitboards[(_segmentLength * 2) .. (_segmentLength * 3)]);

    public WarField(in WarContext context, Span<ulong> allBitboardsMemory, ReadOnlySpan<Coordinate> food, ReadOnlySpan<Coordinate> hazards)
    {
        Width = context.Width;
        Height = context.Height;
        Area = context.Area;
        
        _allBitboards = allBitboardsMemory;
        _segmentLength = allBitboardsMemory.Length / TotalBitboards;

        // Initialize board state
        foreach (ref readonly var coordinate in food) { Food.Set(To1D(in coordinate)); }
        foreach (ref readonly var coordinate in hazards) { Hazard.Set(To1D(in coordinate)); }
    }

    // --- WRITE METHODS ---

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetSnakeBit(ushort position1D) => Snakes.Set(position1D);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UpdateSnakePosition(ushort oldTail, ushort newHead, bool hasEaten)
    {
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
        foreach (var position in body)
        {
            Snakes.Clear(position);
        }
    }

    // --- "HOT PATH" READ METHODS (Safe and highly optimized) ---

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool IsOccupied(ushort position1D)
    {
        if (position1D == ushort.MaxValue) return true;
        var ulongIndex = position1D >> 6;
        var bitMask = 1UL << (position1D & 63);
        
        // Direct span access via GetRawData() for maximum performance.
        var hazardData = Hazard.GetRawData();
        var snakesData = Snakes.GetRawData();
        
        return ((hazardData[ulongIndex] | snakesData[ulongIndex]) & bitMask) != 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool IsFood(ushort position1D)
    {
        if (position1D >= Area) return false;
        // The IsSet method on the Bitboard property is perfectly fine here,
        // as the overhead is minimal and JIT can optimize it well.
        return Food.IsSet(position1D);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool IsHazard(ushort position1D)
    {
        if (position1D >= Area) return false;
        return Hazard.IsSet(position1D);
    }
    
    // --- HELPER METHODS ---

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ushort To1D(in Coordinate coord) => (ushort)(coord.Y * Width + coord.X);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ushort GetNeighbor(ushort position1D, MoveDirection direction)
    {
        return direction switch
        {
            MoveDirection.Up => position1D < Width ? ushort.MaxValue : (ushort)(position1D - Width),
            MoveDirection.Down => position1D >= Area - Width ? ushort.MaxValue : (ushort)(position1D + Width),
            MoveDirection.Left => position1D % Width == 0 ? ushort.MaxValue : (ushort)(position1D - 1),
            MoveDirection.Right => (position1D + 1) % Width == 0 ? ushort.MaxValue : (ushort)(position1D + 1),
            _ => ushort.MaxValue
        };
    }
}