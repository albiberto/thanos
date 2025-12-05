using Thanos.War;

namespace Thanos.Abstract;

public interface ISlotMemoryPool
{
    int Capacity { get; }
    int Count { get; }

    /// <summary>
    /// Configura il pool per il match corrente (numero di serpenti).
    /// </summary>
    void Configure(int snakeCount);

    /// <summary>
    /// Alloca uno slot e ne restituisce l'indice.
    /// </summary>
    int Allocate();

    /// <summary>
    /// Resetta il contatore di allocazione.
    /// </summary>
    void Reset();

    /// <summary>
    /// Ottiene una "vista" Arena sullo slot specificato.
    /// </summary>
    Arena GetArena(int index);

    /// <summary>
    /// Ottiene una "vista" Heuristics sullo slot specificato.
    /// </summary>
    Heuristics GetHeuristics(int index);
}