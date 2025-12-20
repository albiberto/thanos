using Thanos.SourceGen;

namespace Thanos.Shared;

public static class CoordinatesBuilder
{
    public static void Populate(byte width, byte height, Span<Coordinate> memory)
    {
        if (memory.Length != width * height) throw new ArgumentException($"Buffer size mismatch: expected {width * height}, got {memory.Length}");

        var index = 0;
        
        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++)
            memory[index++] = new Coordinate((byte)x, (byte)y);
    }
}