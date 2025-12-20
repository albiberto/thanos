using Thanos.War;

namespace Thanos.Abstract;

public interface ISlotMemoryPool : IDisposable
{
    /// <summary>
    /// Capacità massima (numero di nodi) del pool.
    /// </summary>
    uint Capacity { get; }
    
    /// <summary>
    /// Numero di nodi attualmente allocati (incluso l'indice 0 riservato).
    /// </summary>
    int Index { get; }
    
    /// <summary>
    /// Ottiene una "vista" Arena sullo slot specificato.
    /// </summary>
    Arena GetArena(int index);

    /// <summary>
    /// Ottiene una "vista" Heuristics sullo slot specificato.
    /// </summary>
    Heuristics GetHeuristics(int index);

    /// <summary>
    /// Alloca uno slot e ne restituisce l'indice.
    /// </summary>
    int Allocate();

    /// <summary>
    /// Resetta il contatore di allocazione.
    /// </summary>
    void Reset();
}