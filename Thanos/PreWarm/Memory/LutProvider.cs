using System.Runtime.InteropServices;
using Thanos.Common;
using Thanos.SourceGen;

namespace Thanos.PreWarm.Memory;

/// <summary>
/// Fornisce accesso globale (Singleton) a tutte le Look-Up Tables (LUTs) pre-calcolate.
/// Alloca un singolo blocco di memoria nativa all'avvio e lo gestisce per tutta la durata dell'applicazione.
/// </summary>
public sealed unsafe class LutProvider : IDisposable
{
    public static LutProvider Instance { get; } = new();

    private readonly LutMemoryLayout _smallLayout;
    private readonly LutMemoryLayout _mediumLayout;
    private readonly LutMemoryLayout _largeLayout;
        
    private readonly int _mediumOffset;
    private readonly int _largeOffset;
        
    private readonly byte* _basePointer;

    /// <summary>
/// Il costruttore è privato per forzare l'uso tramite la proprietà statica 'Instance'.
/// </summary>
private LutProvider()
{
    // 1. Calcola il layout per ogni dimensione possibile
    _smallLayout = Build(Constants.Small);
    _mediumLayout = Build(Constants.Medium);
    _largeLayout = Build(Constants.Large);

    // 2. Calcola gli offset di partenza per ogni blocco di LUT
    _mediumOffset = _smallLayout.TotalSize;
    _largeOffset = _smallLayout.TotalSize + _mediumLayout.TotalSize;
        
    // 3. Alloca un singolo blocco di memoria nativa
    var totalMemoryNeeded = (nuint)(_smallLayout.TotalSize + _mediumLayout.TotalSize + _largeLayout.TotalSize);
    _basePointer = (byte*)NativeMemory.AlignedAlloc(totalMemoryNeeded, (nuint)Constants.CacheLine);
        
    // 4. Popola la memoria con i dati delle LUT usando un ciclo (operazione "Burn")
    
    // Creiamo un array di "lavori" da eseguire
    var layoutsToBuild = new[]
    {
        (Area: Constants.Small, Layout: _smallLayout, Offset: 0),
        (Area: Constants.Medium, Layout: _mediumLayout, Offset: _mediumOffset),
        (Area: Constants.Large, Layout: _largeLayout, Offset: _largeOffset)
    };

    // Usiamo un ciclo per evitare codice duplicato
    foreach (var (area, layout, offset) in layoutsToBuild)
    {
        var width = (int)Math.Sqrt(area);
        
        // Crea le Span che puntano alle sezioni corrette della memoria nativa
        var neighborsSpan = new Span<ushort>(_basePointer + offset, layout.NeighborsSize / sizeof(ushort));
        var positionalScoresSpan = new Span<float>(_basePointer + offset + layout.NeighborsSize, layout.PositionalScoresSize / sizeof(float));
        var conversionMapSpan = new Span<Coordinate>(_basePointer + offset + layout.NeighborsSize + layout.PositionalScoresSize, layout.MapSize / sizeof(Coordinate));
        
        // Chiama i metodi di build, che scriveranno direttamente nelle Span
        NeighborsBoardCache.Build(area, width, neighborsSpan);
        PositionalScoreCache.Build(width, positionalScoresSpan);
        ConversionMapCache.Build(area, width, conversionMapSpan);
    }
}

    /// <summary>
    /// Indexer per ottenere le LUT corrette in base all'area della mappa.
    /// </summary>
    public LutPointers this[int area] => area switch
    {
        Constants.Small => new LutPointers(
            nPtr: _basePointer,
            nLen: _smallLayout.NeighborsSize / sizeof(ushort),
            pPtr: _basePointer + _smallLayout.NeighborsSize,
            pLen: _smallLayout.PositionalScoresSize / sizeof(float),
            cPtr: _basePointer + _smallLayout.NeighborsSize + _smallLayout.PositionalScoresSize,
            cLen: _smallLayout.MapSize / sizeof(Coordinate)
        ),
            
        Constants.Medium => new LutPointers(
            nPtr: _basePointer + _mediumOffset,
            nLen: _mediumLayout.NeighborsSize / sizeof(ushort),
            pPtr: _basePointer + _mediumOffset + _mediumLayout.NeighborsSize,
            pLen: _mediumLayout.PositionalScoresSize / sizeof(float),
            cPtr: _basePointer + _mediumOffset + _mediumLayout.NeighborsSize + _mediumLayout.PositionalScoresSize,
            cLen: _mediumLayout.MapSize / sizeof(Coordinate)
        ),

        Constants.Large => new LutPointers(
            nPtr: _basePointer + _largeOffset,
            nLen: _largeLayout.NeighborsSize / sizeof(ushort),
            pPtr: _basePointer + _largeOffset + _largeLayout.NeighborsSize,
            pLen: _largeLayout.PositionalScoresSize / sizeof(float),
            cPtr: _basePointer + _largeOffset + _largeLayout.NeighborsSize + _largeLayout.PositionalScoresSize,
            cLen: _largeLayout.MapSize / sizeof(Coordinate)
        ),

        _ => throw new ArgumentOutOfRangeException(nameof(area), $"LUTs non disponibili per area {area}.")
    };
        
    /// <summary>
    /// Metodo helper che calcola le dimensioni corrette e allineate per un dato set di LUT.
    /// </summary>
    private static LutMemoryLayout Build(int area) =>
        new()
        {
            NeighborsSize = (area * 4 * sizeof(ushort)).AlignUp64(),
            PositionalScoresSize = (area * sizeof(float)).AlignUp64(),
            MapSize = (area * sizeof(Coordinate)).AlignUp64()
        };

    public void Dispose()
    {
        NativeMemory.Free(_basePointer);
        GC.SuppressFinalize(this);
    }

    ~LutProvider() => Dispose();
}