// File: ZobristTable.cs (Versione Corretta)

using System.Text.Json;

namespace Thanos.Common;

public static class ZobristTable
{
    private static readonly long[,] SnakeTable;
    private static readonly long[] DeathTable; // <-- AGGIUNGI QUESTO

    static ZobristTable()
    {
        var random = new Random(12345);
        
        SnakeTable = new long[Constants.MaxSnakesCount, Constants.MaxArea];
        for (var i = 0; i < Constants.MaxSnakesCount; i++)
        {
            for (var j = 0; j < Constants.MaxArea; j++)
            {
                var value = random.NextInt64();
                // Console.WriteLine($"ZobristTable[{i}, {j}] = {value}");
                SnakeTable[i, j] = value;
            }
        }
        
        DeathTable = new long[Constants.MaxSnakesCount];
        for (var i = 0; i < Constants.MaxSnakesCount; i++)
        {
            DeathTable[i] = random.NextInt64();
        }
        
        
    }

    public static long GetSnakeValue(int snakeIndex, ushort position1D) => SnakeTable[snakeIndex, position1D];
    
    public static long GetDeathValue(int snakeIndex) => DeathTable[snakeIndex];
}