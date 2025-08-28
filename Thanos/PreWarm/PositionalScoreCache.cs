namespace Thanos.PreWarm;

public static class PositionalScoreCache
{
    public static void Build(int width, Span<double> scores)
    {
        var area = width * width;
        var centerX = width / 2;
        var centerY = width / 2;

        for (ushort pos = 0; pos < area; pos++)
        {
            var x = pos % width;
            var y = pos / width;

            // --- CORREZIONE: La LUT ora calcola solo il posizionamento Centro vs Bordo ---

            // 1. Penalità per il bordo.
            // Usiamo un gradiente: più sei vicino, peggio è.
            var distBorder = Math.Min(Math.Min(x, width - 1 - x), Math.Min(y, width - 1 - y));
            // Se distBorder è 0, sei sul bordo. Se è 1, sei a una casella dal bordo, ecc.
            var borderScore = 0.0;
            if (distBorder == 0) borderScore = HeuristicWeights.BorderPenaltyValue;
            if (distBorder == 1) borderScore = HeuristicWeights.BorderPenaltyValue / 4; // Penalità ridotta se sei vicino

            // 2. Bonus per il centro
            var distCenter = Math.Abs(x - centerX) + Math.Abs(y - centerY);
            var centerScore = HeuristicWeights.CenterBonusValue / (1 + distCenter);

            // 3. Il punteggio combinato ora è solo posizionale
            scores[pos] = borderScore + centerScore;
        }
    }
}