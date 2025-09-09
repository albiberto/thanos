using System.Runtime.InteropServices;
using Thanos.Common;
using Thanos.SourceGen;

namespace Thanos.PreWarm.Memory;

/// <summary>
///     Fornisce accesso globale (Singleton) a tutte le Look-Up Tables (LUTs) pre-calcolate.
///     Alloca un singolo blocco di memoria nativa all'avvio e lo gestisce per tutta la durata dell'applicazione.
/// </summary>
public sealed unsafe class LutProvider : IDisposable
{
    private readonly byte* _basePointer;
    private readonly LutMemoryLayout _largeLayout;
    private readonly int _largeOffset;
    private readonly LutMemoryLayout _mediumLayout;

    private readonly int _mediumOffset;

    private readonly LutMemoryLayout _smallLayout;

    /// <summary>
    ///     Il costruttore è privato per forzare l'uso tramite la proprietà statica 'Instance'.
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
        _basePointer = (byte*)NativeMemory.AlignedAlloc(totalMemoryNeeded, Constants.CacheLine);

        // 4. Popola la memoria con i dati delle LUT usando un ciclo (operazione "Burn")

        // Creiamo un array di "lavori" da eseguire
        var layoutsToBuild = new[]
        {
            (Area: Constants.Small, Layout: _smallLayout, Offset: 0),
            (Area: Constants.Medium, Layout: _mediumLayout, Offset: _mediumOffset),
            (Area: Constants.Large, Layout: _largeLayout, Offset: _largeOffset)
        };

        foreach (var (area, layout, offset) in layoutsToBuild)
        {
            var width = (int)Math.Sqrt(area);
            var neighbors = new Span<ushort>(_basePointer + offset, area * 4); 

            var positionalScores = new Span<float>(_basePointer + offset + layout.NeighborsSize, area); 
            var conversionMap = new Span<Coordinate>(_basePointer + offset + layout.NeighborsSize + layout.PositionalScoresSize, area);

            NeighborsBoardCache.Build(area, width, neighbors);
            PositionalScoreCache.Build(width, positionalScores);
            ConversionMapCache.Build(area, width, conversionMap);
        }
    }

    public static LutProvider Instance { get; } = new();

    /// <summary>
    ///     Indexer per ottenere le LUT corrette in base all'area della mappa.
    /// </summary>
    public LutPointers this[int area] => area switch
    {
        Constants.Small => new LutPointers(
            _basePointer,
            _smallLayout.NeighborsSize / sizeof(ushort),
            _basePointer + _smallLayout.NeighborsSize,
            _smallLayout.PositionalScoresSize / sizeof(float),
            _basePointer + _smallLayout.NeighborsSize + _smallLayout.PositionalScoresSize,
            _smallLayout.MapSize / sizeof(Coordinate)
        ),

        Constants.Medium => new LutPointers(
            _basePointer + _mediumOffset,
            _mediumLayout.NeighborsSize / sizeof(ushort),
            _basePointer + _mediumOffset + _mediumLayout.NeighborsSize,
            _mediumLayout.PositionalScoresSize / sizeof(float),
            _basePointer + _mediumOffset + _mediumLayout.NeighborsSize + _mediumLayout.PositionalScoresSize,
            _mediumLayout.MapSize / sizeof(Coordinate)
        ),

        Constants.Large => new LutPointers(
            _basePointer + _largeOffset,
            _largeLayout.NeighborsSize / sizeof(ushort),
            _basePointer + _largeOffset + _largeLayout.NeighborsSize,
            _largeLayout.PositionalScoresSize / sizeof(float),
            _basePointer + _largeOffset + _largeLayout.NeighborsSize + _largeLayout.PositionalScoresSize,
            _largeLayout.MapSize / sizeof(Coordinate)
        ),

        _ => throw new ArgumentOutOfRangeException(nameof(area), $"LUTs non disponibili per area {area}.")
    };

    public void Dispose()
    {
        NativeMemory.Free(_basePointer);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    ///     Metodo helper che calcola le dimensioni corrette e allineate per un dato set di LUT.
    /// </summary>
    private static LutMemoryLayout Build(int area) =>
        new()
        {
            NeighborsSize = (area * 4 * sizeof(ushort)).AlignUp64(),
            PositionalScoresSize = (area * sizeof(float)).AlignUp64(),
            MapSize = (area * sizeof(Coordinate)).AlignUp64()
        };

    ~LutProvider() => Dispose();
}