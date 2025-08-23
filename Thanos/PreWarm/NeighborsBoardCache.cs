using System.Collections.Concurrent;

namespace Thanos.PreWarm;

public class NeighborsBoardCache
{
    private static readonly ConcurrentDictionary<int, ushort[]> _cache = [];

    /// <summary>
    /// Gets a MoveLookupTable for the specified square grid size.
    /// </summary>
    public static ReadOnlySpan<ushort> Get(int width) => _cache[width];
    
    public static void Burn(int maxWidth)
    {
        foreach (var width in Enumerable.Range(1, maxWidth)) _cache[width] = Build(width).ToArray();
    }

    // The LUT stores 4 neighbors (U,D,L,R) for each of the 'area' squares.
    private static ushort[] Build(int width)
    {
        var area = width * width;
        var neighbors = new ushort[area * 4];

        for (ushort pos = 0; pos < area; pos++)
        {
            var offset = pos * 4;
            
            // Pre-calculate UP
            neighbors[offset + 0] = pos < width ? ushort.MaxValue : (ushort)(pos - width);
            
            // Pre-calculate DOWN
            neighbors[offset + 1] = pos >= area - width ? ushort.MaxValue : (ushort)(pos + width);

            // Pre-calculate LEFT
            neighbors[offset + 2] = pos % width == 0 ? ushort.MaxValue : (ushort)(pos - 1);
            
            // Pre-calculate RIGHT
            neighbors[offset + 3] = (pos + 1) % width == 0 ? ushort.MaxValue : (ushort)(pos + 1);
        }

        return neighbors;
    }
}