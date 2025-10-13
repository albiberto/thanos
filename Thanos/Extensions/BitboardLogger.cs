using System.Text;
using Thanos.War;

// Assicurati che il namespace della tua Bitboard sia corretto

namespace Thanos.Extensions;

public static class BitboardLogger
{
    public static string ToGridString(this Bitboard bitboard, int width, int height)
    {
        var sb = new StringBuilder();

        // 1. Header con le coordinate X
        sb.Append("   ");
        for (var x = 0; x < width; x++) sb.AppendFormat("{0,-2}", x);
        sb.AppendLine();

        // 2. Bordo superiore
        sb.Append("  +-");
        for (var x = 0; x < width; x++) sb.Append("--");
        sb.AppendLine("-+");

        // 3. Disegna ogni riga della griglia DALL'ALTO VERSO IL BASSO
        //    Questa è l'unica modifica necessaria.
        for (var y = height - 1; y >= 0; y--)
        {
            // Coordinate Y a sinistra
            sb.AppendFormat("{0,2}| ", y);

            for (var x = 0; x < width; x++)
            {
                var position1D = (ushort)(y * width + x);
                sb.Append(bitboard.IsSet(position1D) ? "■ " : "· ");
            }

            sb.AppendLine("|");
        }

        // 4. Bordo inferiore
        sb.Append("  +-");
        for (var x = 0; x < width; x++) sb.Append("--");
        sb.AppendLine("-+");

        return sb.ToString();
    }
}