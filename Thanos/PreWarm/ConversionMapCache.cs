using System.Collections.Concurrent;
using System.Drawing;
using Thanos.SourceGen;

namespace Thanos.PreWarm;

public static class ConversionMapCache
{
    // <--- CORREZIONE: Cambiato il tipo della cache per memorizzare Point
    private static readonly Coordinate[][] _cache;

    /// <summary>
    /// Il costruttore statico viene eseguito una sola volta e in modo thread-safe
    /// prima di qualsiasi altro accesso alla classe.
    /// </summary>
    static ConversionMapCache() => _cache = new Coordinate[Constants.MaxWidth + 1][];
    
    /// <summary>
    /// Ottiene la Lookup Table di conversione 1D->2D per la larghezza specificata.
    /// </summary>
    public static Coordinate[] Get(int width) => _cache[width];
    
    /// <summary>
    /// Pre-calcola e memorizza le mappe di conversione per tutte le larghezze fino a maxWidth.
    /// </summary>
    public static void Burn(int maxWidth)
    {
        // Usiamo Parallel.ForEach per velocizzare il pre-calcolo su macchine multi-core.
        Parallel.ForEach(Enumerable.Range(1, maxWidth), width =>
        {
            _cache[width] = Build(width);
        });
    }
    
    /// <summary>
    /// Costruisce la Lookup Table (LUT) che mappa ogni posizione 1D
    /// alla sua corrispondente coordinata 2D (X, Y).
    /// </summary>
    private static Coordinate[] Build(int width)
    {
        var area = width * width;
        var coordinateLut = new Coordinate[area];

        for (ushort pos1D = 0; pos1D < area; pos1D++)
        {
            // Applica la formula di conversione standard che corrisponde al tuo schema
            var x = (ushort)(pos1D % width);
            var y = (ushort)(pos1D / width);
            
            coordinateLut[pos1D] = new Coordinate(x, y);
        }

        return coordinateLut;
    }
}