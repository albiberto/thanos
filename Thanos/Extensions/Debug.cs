namespace Thanos.Extensions;

public static class Debug
{
    public static unsafe void Print(uint width, uint height, byte[] collisionBytes)
    {
        fixed (byte* ptr = collisionBytes)
        {
            Console.WriteLine("\n💥 Mappa collisioni:");
            // Intestazione colonne
            Console.WriteLine("\n    " + string.Join(" ", Enumerable.Range(1, (int)width).Select(x => $"{x:D2} ")));
            for (uint y = 0; y < height; y++)
            {
                Console.Write($"{y + 1:D2} ");
                for (uint x = 0; x < width; x++)
                {
                    var v = *(ptr + y * width + x);
                    var cell = v == 1 
                        ? "⛔" // Occupato 
                        : "⬛"; // Libero
                    Console.Write($" {cell} ");
                }
                Console.WriteLine();
            }
            Console.WriteLine();
            Console.WriteLine("Legenda:");
            Console.WriteLine("⛔ = Occupato, \n⬛ = Libero");
        }
    }
}