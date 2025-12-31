namespace Thanos.Tests.Integration.WarSnake.Support;

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
    /// Crea un serpente "impilato" (tutti i segmenti sulla stessa cella).
    /// </summary>
    public static ushort[] Stacked(int length, int width, int height, SnakePlacement placement)
    {
        var body = new ushort[length];
        
        // Calcolo posizione centrale (int division tronca)
        var headX = width / 2;
        var headY = height / 2;
        var position = (ushort)(headY * width + headX);

        // Riempimento array veloce
        Array.Fill(body, position);

        return body;
    }

    /// <summary>
    /// Crea un serpente "lineare" (dritta linea retta).
    /// Il corpo si estende nella direzione OPPOSTA al facing (dietro la testa).
    /// </summary>
    public static ushort[] Linear(int length, int width, int height, SnakeFacing facing)
    {
        var body = new ushort[length];
        var headX = width / 2;
        var headY = height / 2;

        // Determina la direzione in cui si estende il CORPO (opposta alla testa)
        // Esempio: Se guardo UP (Y+), il corpo scende (Y-).
        int dx = 0, dy = 0;
        switch (facing)
        {
            case SnakeFacing.Up:    dy = -1; break; // Collo sotto la testa
            case SnakeFacing.Down:  dy = +1; break; // Collo sopra la testa
            case SnakeFacing.Left:  dx = +1; break; // Collo a destra
            case SnakeFacing.Right: dx = -1; break; // Collo a sinistra
            default: throw new ArgumentOutOfRangeException(nameof(facing), facing, null);
        }

        for (var i = 0; i < length; i++)
        {
            var x = headX + (dx * i);
            var y = headY + (dy * i);
            
            // Check opzionale per evitare out of bounds nei test se la length è eccessiva
            // if (x < 0 || x >= width || y < 0 || y >= height) ... 
            
            body[i] = (ushort)(y * width + x);
        }

        return body;
    }
}