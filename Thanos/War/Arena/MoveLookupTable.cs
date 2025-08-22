using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Thanos.MCST;

namespace Thanos.War.Grid;

/// <summary>
/// A pre-computed lookup table for grid neighbors.
/// This struct pre-calculates all possible moves from every square on the grid,
/// eliminating all conditional logic from the hot path.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public readonly struct MoveLookupTable
{
    private readonly ushort[] _neighbors;
    private readonly int _width;

    public MoveLookupTable(int width, int height)
    {
        _width = width;
        var area = width * height;
        // The LUT stores 4 neighbors (U,D,L,R) for each of the 'area' squares.
        _neighbors = new ushort[area * 4];

        for (ushort pos = 0; pos < area; pos++)
        {
            var offset = pos * 4;
            
            // Pre-calculate UP
            _neighbors[offset + 0] = pos < width ? ushort.MaxValue : (ushort)(pos - width);
            
            // Pre-calculate DOWN
            _neighbors[offset + 1] = pos >= area - width ? ushort.MaxValue : (ushort)(pos + width);

            // Pre-calculate LEFT
            _neighbors[offset + 2] = pos % width == 0 ? ushort.MaxValue : (ushort)(pos - 1);
            
            // Pre-calculate RIGHT
            _neighbors[offset + 3] = (pos + 1) % width == 0 ? ushort.MaxValue : (ushort)(pos + 1);
        }
    }

    /// <summary>
    /// Gets the pre-calculated neighbor for a given position and move.
    /// This method is branchless.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort GetNeighbor(ushort position, byte move)
    {
        // Convert move bitmask (1,2,4,8) to an index (0,1,2,3)
        // BitOperations.TrailingZeroCount is a CPU intrinsic and extremely fast.
        var moveIndex = BitOperations.TrailingZeroCount(move);
        return _neighbors[position * 4 + moveIndex];
    }
}