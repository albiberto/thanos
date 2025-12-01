namespace Thanos.Shared;

public static class NeighborsGridBuilder
{
    public static void Build(byte width, Span<ushort> memory)
    {
        if (memory.Length % 4 != 0) throw new ArgumentException("La lunghezza dello Span di memoria deve essere un multiplo di 4.", nameof(memory));

        var area = memory.Length / 4;

        for (var pos = 0; pos < area; pos++)
        {
            var offset = pos * 4;

            // UP
            memory[offset + 0] = pos >= area - width ? ushort.MaxValue : (ushort)(pos + width);
            // DOWN
            memory[offset + 1] = pos < width ? ushort.MaxValue : (ushort)(pos - width);
            // LEFT
            memory[offset + 2] = pos % width == 0 ? ushort.MaxValue : (ushort)(pos - 1);
            // RIGHT
            memory[offset + 3] = (pos + 1) % width == 0 ? ushort.MaxValue : (ushort)(pos + 1);
        }
    }
}