namespace Thanos.Tests.Integration.Support;

public static class BodyBuilder
{
    public static ushort[] ZigZag(int length, int width, int height, SnakeStartCorner corner)
    {
        var body = new ushort[length];
        var startFromTop = corner is SnakeStartCorner.TopLeft or SnakeStartCorner.TopRight;
        var startFromRight = corner is SnakeStartCorner.BottomRight or SnakeStartCorner.TopRight;

        for (var i = 0; i < length; i++)
        {
            var row = i / width;
            var col = i % width;

            var y = startFromTop ? height - 1 - row : row;

            var invertX = row % 2 != 0;
            if (startFromRight) invertX = !invertX;
            var x = invertX ? width - 1 - col : col;

            body[i] = (ushort)(y * width + x);
        }

        return body;
    }

    /// <summary>
    ///     Crea un serpente "impilato" (tutti i segmenti sulla stessa cella centrale).
    /// </summary>
    public static ushort[] Stacked(int length, int width, int height, SnakePlacement placement)
    {
        var body = new ushort[length];
        var headX = width / 2;
        var headY = height / 2;
        var position = (ushort)(headY * width + headX);

        Array.Fill(body, position);
        return body;
    }

    /// <summary>
    ///     Crea un serpente "lineare" (dritto).
    ///     Calcola automaticamente una posizione sicura per la testa affinché il corpo rientri nella griglia.
    /// </summary>
    public static ushort[] Linear(int length, int width, int height, SnakeFacing facing)
    {
        var body = new ushort[length];

        // Determina la direzione del CORPO (opposta al facing della testa)
        int dx = 0, dy = 0;
        switch (facing)
        {
            case SnakeFacing.Up: dy = -1; break; // Il corpo scende
            case SnakeFacing.Down: dy = 1; break; // Il corpo sale
            case SnakeFacing.Left: dx = 1; break; // Il corpo va a destra
            case SnakeFacing.Right: dx = -1; break; // Il corpo va a sinistra
            default: throw new ArgumentOutOfRangeException(nameof(facing), facing, null);
        }

        // --- SAFE START CALCULATION ---
        // Invece di partire dal centro, calcoliamo un punto che garantisca spazio "dietro".
        // Se il corpo va a Y- (scende), la testa deve essere abbastanza in alto.
        // Se il corpo va a Y+ (sale), la testa deve essere abbastanza in basso.

        var headX = width / 2;
        var headY = height / 2;

        // Correzione dinamica per evitare OutOfBounds
        if (dy < 0) headY = Math.Max(headY, length - 1); // Deve avere spazio sotto
        if (dy > 0) headY = Math.Min(headY, height - length); // Deve avere spazio sopra
        if (dx < 0) headX = Math.Max(headX, length - 1); // Deve avere spazio a sinistra
        if (dx > 0) headX = Math.Min(headX, width - length); // Deve avere spazio a destra

        for (var i = 0; i < length; i++)
        {
            var x = headX + dx * i;
            var y = headY + dy * i;

            // Safety Check
            if (x < 0 || x >= width || y < 0 || y >= height)
                throw new InvalidOperationException($"BodyBuilder generated OOB coordinates: ({x},{y}) for Length {length} on {width}x{height}");

            body[i] = (ushort)(y * width + x);
        }

        return body;
    }
}