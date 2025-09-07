using System.Runtime.CompilerServices;

namespace Thanos.Common;

/// <summary>
/// Contiene le tabelle di valori casuali a 64-bit per l'hashing Zobrist.
/// La tabella viene inizializzata una sola volta all'avvio dell'applicazione.
/// </summary>
public static class ZobristTable
{
    // --- Costanti di Configurazione ---
    private const int MaxSnakes = 8;      // Il numero massimo di serpenti che il tuo pool può gestire
    private const int MaxGridArea = 19 * 19;  // La dimensione massima della griglia (es. 16x16)
    private const int Seed = 42;          // Un seme fisso per garantire la riproducibilità

    // --- Le Tabelle di Hashing ---
    // Queste tabelle conterranno i nostri numeri casuali pre-calcolati.
    private static readonly long[,] _snakeValues;
    private static readonly long[] _foodValues;
    private static readonly long[] _hazardValues;

    /// <summary>
    /// Costruttore statico. Viene eseguito automaticamente dal runtime .NET
    /// una sola volta, prima che qualsiasi membro di questa classe venga utilizzato.
    /// È il posto perfetto per l'inizializzazione.
    /// </summary>
    static ZobristTable()
    {
        var random = new Random(Seed);

        // Inizializza le tabelle con le dimensioni massime
        _snakeValues = new long[MaxSnakes, MaxGridArea];
        _foodValues = new long[MaxGridArea];
        _hazardValues = new long[MaxGridArea];

        // Popola la tabella dei serpenti
        for (var i = 0; i < MaxSnakes; i++)
        {
            for (var j = 0; j < MaxGridArea; j++)
            {
                _snakeValues[i, j] = random.NextInt64();
            }
        }
        
        // Popola la tabella del cibo
        for (var i = 0; i < MaxGridArea; i++)
        {
            _foodValues[i] = random.NextInt64();
        }
        
        // Popola la tabella degli ostacoli
        for (var i = 0; i < MaxGridArea; i++)
        {
            _hazardValues[i] = random.NextInt64();
        }
    }

    // --- Metodi di Accesso Pubblici ---
    // Questi metodi sono estremamente veloci. Saranno inlinati dal compilatore
    // e si tradurranno in un singolo accesso in memoria all'array.

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long GetSnakeValue(int snakeIndex, ushort pos) => _snakeValues[snakeIndex, pos];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long GetFoodValue(ushort pos) => _foodValues[pos];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long GetHazardValue(ushort pos) => _hazardValues[pos];
}