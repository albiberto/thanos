using Thanos.Enums;

namespace Thanos.MCST;

/// <summary>
/// Contiene la tabella di hash Zobrist per l'intero gioco.
/// Viene inizializzata staticamente una sola volta all'avvio del programma.
/// </summary>
public static class ZobristTable
{
    // Indici per accedere alla tabella in modo leggibile
    private const int SnakeIdDimension = 0;
    private const int PositionDimension = 1;

    // La tabella principale: [Tipo di pezzo, Posizione del pezzo]
    // In questo caso, il "tipo" di pezzo include l'ID del serpente.
    // [Constants.MaxSnakeCount, Constants.MaxArea]
    private static readonly long[,] Table;

    static ZobristTable()
    {
        // Usa un seme fisso per la riproducibilità, utile per il debug.
        // In produzione, potresti usare un seme casuale.
        var random = new Random(12345);
        
        Table = new long[Constants.MaxSnakeCount, Constants.MaxArea];

        for (var i = 0; i < Constants.MaxSnakeCount; i++)
        {
            for (var j = 0; j < Constants.MaxArea; j++)
            {
                Table[i, j] = random.NextInt64();
            }
        }
    }

    /// <summary>
    /// Ottiene il valore Zobrist per un serpente specifico in una data posizione.
    /// </summary>
    public static long GetSnakeValue(int snakeIndex, ushort position1D)
    {
        // Aggiungiamo controlli per la sicurezza durante il debug.
        // In release, potresti rimuoverli se sei sicuro dei tuoi indici.
        if (snakeIndex >= Constants.MaxSnakeCount || position1D >= Constants.MaxArea)
        {
            // Gestisci l'errore o torna 0 per evitare crash
            return 0; 
        }
        return Table[snakeIndex, position1D];
    }
}