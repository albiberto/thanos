using Thanos.War;

namespace Thanos.PreWarm;

public static class PositionalScoreBuilder
{
    public static void Build(int width, Span<float> scores)
    {
        var centerX = width / 2f;
        var centerY = width / 2f;

        for (ushort pos = 0; pos < scores.Length; pos++)
        {
            var x = pos % width;
            var y = pos / width;

            var distBorder = Math.Min(Math.Min(x, width - 1 - x), Math.Min(y, width - 1 - y));

            var borderScore = distBorder switch
            {
                0 => HeuristicsConstants.BorderPenaltyValue,
                1 => HeuristicsConstants.BorderPenaltyValue / 4f,
                _ => 0.0f
            };

            var distCenter = Math.Abs(x - centerX) + Math.Abs(y - centerY);
            var centerScore = HeuristicsConstants.CenterBonusValue / (1f + distCenter);

            scores[pos] = borderScore + centerScore;
        }
    }
}