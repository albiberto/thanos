using Thanos.SourceGen;

namespace Thanos.PreWarm.Memory;

public readonly unsafe struct LutMemoryLayout
{
    public readonly nuint TotalSizeInBytes;
    public readonly LutInfo[] PositionalScoreLayout;
    public readonly LutInfo[] ConversionMapLayout;

    public LutMemoryLayout(int maxWidth, int maxArea)
    {
        PositionalScoreLayout = new LutInfo[maxWidth + 1];
        ConversionMapLayout = new LutInfo[maxWidth + 1];

        nuint offset = 0;
        for (var width = 1; width <= maxWidth; width++)
        {
            // Layout per Positional Scores
            PositionalScoreLayout[width] = new LutInfo((int)offset, maxArea);
            offset += (nuint)maxArea * sizeof(double);

            // Layout per Conversion Map
            ConversionMapLayout[width] = new LutInfo((int)offset, maxArea);
            offset += (nuint)(maxArea * sizeof(Coordinate));
        }

        TotalSizeInBytes = offset;
    }
}