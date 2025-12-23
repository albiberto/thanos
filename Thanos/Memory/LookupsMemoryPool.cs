using System.Runtime.InteropServices;
using Thanos.Abstract;
using Thanos.Shared;
using Thanos.SourceGen;

namespace Thanos.Memory;

public sealed unsafe class LookupsMemoryPool : ILookupsMemoryPool
{
    private bool _disposed; // Flag di sicurezza
    
    // --- SINGLETON STATICO (Aggiunto) ---
    // Usiamo Lazy per garantire l'inizializzazione thread-safe e ritardata
    private static readonly Lazy<LookupsMemoryPool> _mediumInstance = new(() => 
        new LookupsMemoryPool(Constants.Medium.Width, Constants.Medium.Height, Constants.Medium.Area));

    // Access point globale usato dal Bootstrapper
    public static LookupsMemoryPool Medium => _mediumInstance.Value;

    // --- MEMBRI DI ISTANZA ---
    private byte* _basePointer;
    private readonly LookupsMemoryLayout _layout;

    public CoordinatesMatrix CoordinatesMatrix => new(CoordinatesSpan);
    public NeighborsMatrix NeighborsMatrix => new(NeighborsSpan);
    
    private ReadOnlySpan<Coordinate> CoordinatesSpan => 
        new(_basePointer + _layout.Coordinates.Offset, _layout.Coordinates.Count<Coordinate>());
    
    private ReadOnlySpan<ushort> NeighborsSpan => 
        new(_basePointer + _layout.Neighbors.Offset, _layout.Neighbors.Count<ushort>());

    // Costruttore (Può rimanere public se vuoi poter creare pool di dimensioni diverse in futuro, 
    // oppure private se vuoi forzare l'uso del Singleton Medium)
    public LookupsMemoryPool(byte width, byte height, ushort area)
    {
        _layout = new LookupsMemoryLayout(area);
        
        _basePointer = (byte*)NativeMemory.AlignedAlloc(_layout.TotalSize, Constants.CacheLine);
        NativeMemory.Clear(_basePointer, _layout.TotalSize);

        var coordsSpan = new Span<Coordinate>(_basePointer + _layout.Coordinates.Offset, _layout.Coordinates.Count<Coordinate>());
        var neighborsSpan = new Span<ushort>(_basePointer + _layout.Neighbors.Offset, _layout.Neighbors.Count<ushort>());
            
        CoordinatesBuilder.Populate(width, height, coordsSpan);
        NeighborsBuilder.Populate(width, height, neighborsSpan);
    }

    public void Dispose()
    {
        // Se è già stato disposto, esci subito. Non tentare di liberare di nuovo la memoria.
        if (_disposed) return;
        
        if (_basePointer != null)
        {
            NativeMemory.AlignedFree(_basePointer);
            _basePointer = null;
        }
        
        _disposed = true;
    }
}