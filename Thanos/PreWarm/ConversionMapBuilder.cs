using Thanos.SourceGen;

namespace Thanos.PreWarm;

public static class ConversionMapBuilder
{
    public static void Build(byte width, Span<Coordinate> coordinates)
    {
        for (var pos1D = 0; pos1D < coordinates.Length; pos1D++)
        {
            var x = (byte)(pos1D % width);
            var y = (byte)(pos1D / width);
            coordinates[pos1D] = new Coordinate(x, y);
        }
    }
}