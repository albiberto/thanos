using Thanos.Domain;
using Thanos.Model;

namespace Thanos.Tests.Support;

public static class Debug
{
    /// <summary>
    /// Stampa la griglia di gioco con tutti gli elementi visualizzati
    /// </summary>
    public static void Print(uint width, uint height, uint headX, uint headY, Point[] myBody, Point[] hazards, Snake[] snakes, int validMoves = -1, string testName = "")
    {
        Console.WriteLine($"\n=== {testName} ===");
        Console.WriteLine($"Griglia: {width}x{height}");
        Console.WriteLine($"Testa: ({headX}, {headY})");
        Console.WriteLine($"Corpo serpente: {string.Join(", ", myBody.Select(p => $"({p.x},{p.y})"))}");
        Console.WriteLine($"Hazards: {string.Join(", ", hazards.Select(p => $"({p.x},{p.y})"))}");
        
        if (snakes.Length > 0)
        {
            Console.WriteLine("Serpenti nemici:");
            foreach (var snake in snakes)
            {
                Console.WriteLine($"  - {snake.id}: {string.Join(", ", snake.body.Select(p => $"({p.x},{p.y})"))}");
            }
        }
        
        // Mostra le possibili mosse dalla posizione attuale
        Console.WriteLine($"\nPossibili mosse da ({headX}, {headY}):");
        Console.WriteLine($"UP (0): {(headY > 0 ? $"({headX}, {headY - 1})" : "FUORI GRIGLIA")}");
        Console.WriteLine($"DOWN (2): {(headY < height - 1 ? $"({headX}, {headY + 1})" : "FUORI GRIGLIA")}");
        Console.WriteLine($"LEFT (4): {(headX > 0 ? $"({headX - 1}, {headY})" : "FUORI GRIGLIA")}");
        Console.WriteLine($"RIGHT (8): {(headX < width - 1 ? $"({headX + 1}, {headY})" : "FUORI GRIGLIA")}");
        
        Console.WriteLine("\nCombinazioni mosse valide:");
        Console.WriteLine("1 =>  UP");
        Console.WriteLine("2 =>  DOWN");
        Console.WriteLine("3 =>  UP | DOWN");
        Console.WriteLine("4 =>  LEFT");
        Console.WriteLine("5 =>  UP | LEFT");
        Console.WriteLine("6 =>  DOWN | LEFT");
        Console.WriteLine("7 =>  UP | DOWN | LEFT");
        Console.WriteLine("8 =>  RIGHT");
        Console.WriteLine("9 =>  UP | RIGHT");
        Console.WriteLine("10 => DOWN | RIGHT");
        Console.WriteLine("11 => UP | DOWN | RIGHT");
        Console.WriteLine("12 => LEFT | RIGHT");
        Console.WriteLine("13 => UP | LEFT | RIGHT");
        Console.WriteLine("14 => DOWN | LEFT | RIGHT");
        Console.WriteLine("15 => UP | DOWN | LEFT | RIGHT");
        
        Console.WriteLine();
        Console.WriteLine($"✅ Mosse valide: {GetDirectionIconString(validMoves)}");
        Console.WriteLine();
        
        // Legenda
        Console.WriteLine("Legenda:");
        Console.WriteLine("👽 = Testa del tuo serpente");
        Console.WriteLine("💲 = Corpo del tuo serpente");
        Console.WriteLine("😈 = Testa serpente nemico");
        Console.WriteLine("⛔ = Corpo serpente nemico");
        Console.WriteLine("💀 = Hazard");
        Console.WriteLine("⬛ = Spazio vuoto");
        
        Console.WriteLine("\n🗺️ Mappa:");
        
        // Stampa la griglia con icone
        var grid = new string[height, width];
        // Inizializza tutto vuoto (grigio scuro)
        for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
                grid[y, x] = "⬛";

        // Hazards
        foreach (var h in hazards)
            grid[h.y, h.x] = "💀";

        // Corpo nemici
        foreach (var s in snakes)
        {
            for (var i = 0; i < s.body.Length; i++)
            {
                var p = s.body[i];
                if (i == 0)
                    grid[p.y, p.x] = "😈"; // Testa nemico (palla rossa)
                else
                    grid[p.y, p.x] = "⛔"; // Corpo nemico (quadrato rosso tenue)
            }
        }

        // Corpo mio (sovrascrive nemici se overlap)
        for (var i = 0; i < myBody.Length; i++)
        {
            var p = myBody[i];
            if (i == 0)
                grid[p.y, p.x] = "👽"; // Testa mia (palla blu)
            else
                grid[p.y, p.x] = "💲"; // Corpo mio (quadrato blu)
        }

        // Stampa la griglia allineata
        Console.WriteLine("\n    " + string.Join("  ", Enumerable.Range(0, (int)width).Select(x => x.ToString("D2"))));
        for (var y = 0; y < height; y++)
        {
            Console.Write($"{y:D2} ");
            for (var x = 0; x < width; x++)
                Console.Write(grid[y, x] + "  ");
            Console.WriteLine();
        }
    }
    
    /// <summary>
    /// Converte il bitmask delle direzioni in una stringa leggibile
    /// </summary>
    private static string GetDirectionString(int directions)
    {
        var parts = new List<string>();
        
        if ((directions & MonteCarlo.UP) != 0) parts.Add("UP");
        if ((directions & MonteCarlo.DOWN) != 0) parts.Add("DOWN");
        if ((directions & MonteCarlo.LEFT) != 0) parts.Add("LEFT");
        if ((directions & MonteCarlo.RIGHT) != 0) parts.Add("RIGHT");
        
        return parts.Count > 0 ? string.Join(" | ", parts) : "NESSUNA";
    }
    
    private static string GetDirectionIconString(int directions)
    {
        var parts = new List<string>();

        if ((directions & MonteCarlo.UP) != 0) parts.Add("⬆️ UP");
        if ((directions & MonteCarlo.DOWN) != 0) parts.Add("⬇️ DOWN");
        if ((directions & MonteCarlo.LEFT) != 0) parts.Add("⬅️ LEFT");
        if ((directions & MonteCarlo.RIGHT) != 0) parts.Add("➡️ RIGHT");

        return parts.Count > 0 ? string.Join(" | ", parts) : "NESSUNA";
    }
}