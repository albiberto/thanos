namespace Thanos.PreWarm;

public static class NeighborsBoardCache
{
    /// <summary>
    /// Popola una Span con la LUT dei vicini per una data larghezza.
    /// Scrive direttamente nella memoria fornita, senza allocazioni.
    /// </summary>
    public static void Build(int area, int width, Span<ushort> neighbors)
    {
        if (neighbors.Length != area * 4)
            throw new ArgumentException("La dimensione della Span non è corretta.", nameof(neighbors));

        for (ushort pos = 0; pos < area; pos++)
        {
            var offset = pos * 4;

            // UP
            neighbors[offset + 0] = pos >= area - width ? ushort.MaxValue : (ushort)(pos + width);
            // DOWN
            neighbors[offset + 1] = pos < width ? ushort.MaxValue : (ushort)(pos - width);
            // LEFT
            neighbors[offset + 2] = pos % width == 0 ? ushort.MaxValue : (ushort)(pos - 1);
            // RIGHT
            neighbors[offset + 3] = (pos + 1) % width == 0 ? ushort.MaxValue : (ushort)(pos + 1);
        }
    }
}