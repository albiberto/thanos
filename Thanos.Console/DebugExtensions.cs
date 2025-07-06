using Thanos.Model;

namespace Thanos.Console;

public static class DebugExtensions
{
public static void Print(this Board board)
{
    System.Console.WriteLine("Mappa della board:");
    System.Console.Write("   ");
    for (var x = 0; x < board.width; x++)
        System.Console.Write($"{x,2} ");
    System.Console.WriteLine();
    System.Console.Write("   ");
    for (var x = 0; x < board.width; x++)
        System.Console.Write("---");
    System.Console.WriteLine();

    for (var y = 0; y < board.height; y++)
    {
        System.Console.Write($"{y,2}|");
        for (var x = 0; x < board.width; x++)
        {
            bool isHazard = board.hazards.Any(h => h.x == x && h.y == y);
            bool isSnake = board.snakes.Any(s => s.body.Any(p => p.x == x && p.y == y));
            if (isSnake)
                System.Console.Write(" S ");
            else if (isHazard)
                System.Console.Write(" # ");
            else
                System.Console.Write(" . ");
        }
        System.Console.WriteLine("|");
    }
    System.Console.Write("   ");
    for (var x = 0; x < board.width; x++)
        System.Console.Write("---");
    System.Console.WriteLine();
}
}