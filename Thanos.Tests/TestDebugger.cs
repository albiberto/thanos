using Thanos.Domain;
using Thanos.Model;

namespace Thanos.Tests;

public static class GridDebugger
{
    /// <summary>
    /// Stampa la griglia di gioco con tutti gli elementi visualizzati
    /// </summary>
    public static void PrintGrid(uint width, uint height, uint headX, uint headY, Point[] myBody, Point[] hazards, Snake[] snakes, int validMoves = -1, string testName = "")
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
        
        // Legenda
        Console.WriteLine("\nLegenda:");
        Console.WriteLine("H = Testa del tuo serpente");
        Console.WriteLine("B = Corpo del tuo serpente");
        Console.WriteLine("E = Testa serpente nemico");
        Console.WriteLine("e = Corpo serpente nemico");
        Console.WriteLine("☠ = Hazard");
        Console.WriteLine("· = Spazio vuoto");
        
        Console.WriteLine();
        
        // Crea la griglia
        var grid = new char[height, width];
        
        // Riempie con spazi vuoti
        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++) 
            grid[y, x] = '·';
        
        // Aggiunge gli hazards
        foreach (var hazard in hazards)
            if (hazard.x < width && hazard.y < height) grid[hazard.y, hazard.x] = '☠';
        
        // Aggiunge i serpenti nemici
        foreach (var snake in snakes)
        {
            for (var i = 0; i < snake.body.Length; i++)
            {
                var segment = snake.body[i];
                if (segment.x < width && segment.y < height)
                    grid[segment.y, segment.x] = i == 0 
                        ? 'E' 
                        : 'e'; // Testa del nemico
            }
        }
        
        // Aggiunge il mio serpente
        for (var i = 0; i < myBody.Length; i++)
        {
            var segment = myBody[i];
            if (segment.x < width && segment.y < height) 
                grid[segment.y, segment.x] = i == 0 
                    ? 'H' 
                    : 'B'; // Testa
        }
        
        // Aggiunge la posizione della testa attuale (se diversa dal primo elemento del corpo)
        if (headX < width && headY < height) grid[headY, headX] = 'H';
        
        // Stampa la griglia
        // Header con numeri di colonna
        Console.Write("   ");
        for (var x = 0; x < width; x++) Console.Write($"{x % 10} ");
        Console.WriteLine();
        
        // Stampa ogni riga
        for (var y = 0; y < height; y++)
        {
            Console.Write($"{y:D2} ");
            for (var x = 0; x < width; x++) Console.Write($"{grid[y, x]} ");
            Console.WriteLine();
        }
        
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
        var validMovesString = validMoves < 0 ? "Nessuna" : GetDirectionString(validMoves);
        Console.WriteLine($"Mosse valide: {validMovesString}");
        Console.WriteLine();
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
}