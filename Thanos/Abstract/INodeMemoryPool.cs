namespace Thanos.Abstract;

using MCST;

public interface INodeMemoryPool : IDisposable
{
    /// <summary>
    /// Capacità massima (numero di nodi) del pool.
    /// </summary>
    uint Capacity { get; }

    /// <summary>
    /// Numero di nodi attualmente allocati (incluso l'indice 0 riservato).
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Ottiene un riferimento al nodo all'indice specificato.
    /// </summary>
    ref Node Get(int index);

    /// <summary>
    /// Alloca il prossimo slot disponibile e restituisce il suo indice.
    /// Ritorna -1 se il pool è pieno.
    /// </summary>
    int Allocate();

    /// <summary>
    /// Resetta il puntatore di allocazione a 1 (0 è riservato).
    /// Rende il pool pronto per il turno successivo senza deallocare la memoria.
    /// </summary>
    void Reset();
}