using Thanos.SourceGen;

namespace Thanos.PreWarm;

public static class ConversionMapCache
{
    public static void Build(int width, Span<Coordinate> coordinates)
    {
        var area = width * width;

        for (ushort pos1D = 0; pos1D < area; pos1D++)
        {
            var x = (ushort)(pos1D % width);
            var y = (ushort)(pos1D / width);

            coordinates[pos1D] = new Coordinate(x, y);
        }
    }
}