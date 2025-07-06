namespace Thanos.Extensions;

public static class DebugExtensions
{
    public static void Print(this bool[,] matrix, uint width, uint height)
    {
        Console.WriteLine("Matrice delle Collisioni:");
        Console.Write("   ");
        for (var x = 0; x < width; x++)
            Console.Write($"{x,2} ");
        Console.WriteLine();
        Console.Write("   ");
        for (var x = 0; x < width; x++)
            Console.Write("---");
        Console.WriteLine();

        for (var y = 0; y < height; y++)
        {
            Console.Write($"{y,2}|");
            for (var x = 0; x < width; x++)
                Console.Write($" {(matrix[y, x] ? "#" : ".")} ");
            Console.WriteLine("|");
        }
        Console.Write("   ");
        for (var x = 0; x < width; x++)
            Console.Write("---");
        Console.WriteLine();
    }
}