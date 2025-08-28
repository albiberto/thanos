namespace Thanos.PreWarm;

public static class PositionalScoreCache
{
    // Spostiamo i pesi qui, così la logica è tutta in un posto
    private const double BorderPenaltyValue = -200.0;
    private const double CenterBonusValue = 5.0;
    private const double MobilityBonusValue = 1.0; // Uguale al tuo vecchio MobilityWeight

    public static void Build(int width, Span<double> scores)
    {
        var area = width * width;
        var centerX = width / 2;
        var centerY = width / 2; // Assumendo griglia quadrata

        for (ushort pos = 0; pos < area; pos++)
        {
            var x = pos % width;
            var y = pos / width;

            // 1. Punteggio Posizionale (Bordo + Centro)
            var distBorder = Math.Min(Math.Min(x, width - 1 - x), Math.Min(y, width - 1 - y));
            var borderScore = distBorder == 0 ? BorderPenaltyValue : 0;

            var distCenter = Math.Abs(x - centerX) + Math.Abs(y - centerY);
            var centerScore = CenterBonusValue / (1 + distCenter);

            // 2. Punteggio Mobilità di Base (quante uscite ha una casella vuota)
            var exits = 4;
            if (x == 0 || x == width - 1) exits--;
            if (y == 0 || y == width - 1) exits--;
            var mobilityScore = exits * MobilityBonusValue;

            // 3. Salva il punteggio combinato pre-calcolato
            scores[pos] = borderScore + centerScore + mobilityScore;
        }
    }
}