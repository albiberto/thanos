// File: ZobristTable.cs (Versione Corretta)

namespace Thanos.Common;

public static class ZobristTable
{
    private static readonly long[,] SnakeTable;

    static ZobristTable()
    {
        var random = new Random(12345);
        
        SnakeTable = new long[Constants.MaxSnakesCount, Constants.MaxArea];
        for (var i = 0; i < Constants.MaxSnakesCount; i++)
        {
            for (var j = 0; j < Constants.MaxArea; j++)
            {
                SnakeTable[i, j] = random.NextInt64();
            }
        }
    }

    public static long GetSnakeValue(int snakeIndex, ushort position1D) => SnakeTable[snakeIndex, position1D];
}