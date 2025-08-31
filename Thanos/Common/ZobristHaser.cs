using System.Reflection.Metadata;
using Thanos;
using Thanos.Common;
using Thanos.SourceGen;
using Thanos.War;

public static class ZobristHasher
{
    /// <summary>
    /// Calcola l'hash partendo da un'Arena.
    /// Questa versione è compatibile con l'implementazione del corpo come circular buffer.
    /// </summary>
    public static long CalculateHash(in WarArena arena)
    {
        long hash = 0;
        
        // --- Hash del nostro serpente (Me) ---
        var me = arena.Me;
        if (!me.Dead)
        {
            // Otteniamo i due span che rappresentano il corpo
            me.GetSpans(out var meBodyPart1, out var meBodyPart2);

            // Iteriamo sulla prima parte del corpo
            foreach (var part in meBodyPart1)
            {
                hash ^= ZobristTable.GetSnakeValue(me.Id, part);
            }
            
            // Iteriamo sulla seconda parte del corpo (se esiste)
            foreach (var part in meBodyPart2)
            {
                hash ^= ZobristTable.GetSnakeValue(me.Id, part);
            }
        }
        
        // --- Hash dei nemici (Enemies) ---
        for (int i = 0; i < arena.Enemies.Count; i++)
        {
            var enemy = arena.Enemies[i];
            if (!enemy.Dead)
            {
                // Facciamo la stessa cosa per ogni nemico
                enemy.GetSpans(out var enemyBodyPart1, out var enemyBodyPart2);

                foreach (var part in enemyBodyPart1)
                {
                    hash ^= ZobristTable.GetSnakeValue(enemy.Id, part);
                }
                
                foreach (var part in enemyBodyPart2)
                {
                    hash ^= ZobristTable.GetSnakeValue(enemy.Id, part);
                }
            }
        }
        
        return hash;
    }

    /// <summary>
    /// Calcola l'hash partendo da una Request.
    /// Questo metodo rimane INVARIATO perché opera sui dati della Request,
    /// che espongono il corpo come un semplice array (snake.Body) e non come un circular buffer.
    /// </summary>
    public static long CalculateHash(in Request request, Dictionary<string, int> snakeIdMap)
    {
        long hash = 0;
        int width = request.Board.Width;

        foreach (var snake in request.Board.Snakes)
        {
            int snakeId = snakeIdMap[snake.Id];
            foreach (var part in snake.Body)
            {
                var pos1D = (ushort)(part.Y * width + part.X);
                hash ^= ZobristTable.GetSnakeValue(snakeId, pos1D);
            }
        }
        
        return hash;
    }
}