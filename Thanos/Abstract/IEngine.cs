using Thanos.SourceGen;
using Thanos.MCST;

namespace Thanos.Abstract;

public interface IEngine
{
    /// <summary>
    /// Configura l'engine per il match corrente, fornendo il mapping degli ID dei serpenti.
    /// </summary>
    void InitializeGame(string[] sortedSnakeIds, int count);

    /// <summary>
    /// Esegue la ricerca MCTS e restituisce l'indice del miglior nodo (mossa) trovato.
    /// </summary>
    /// <param name="request">La richiesta corrente di gioco.</param>
    /// <param name="lastChosenIndex">L'indice del nodo scelto al turno precedente (Tree Reuse).</param>
    /// <param name="targetHash">L'hash Zobrist dello stato corrente.</param>
    int FindBestMove(in Request request, int lastChosenIndex, long targetHash);

    /// <summary>
    /// Estrae le statistiche di visita e score dei figli del nodo root corrente.
    /// </summary>
    void GetRootStats(List<RootMoveStat> outputBuffer);

    /// <summary>
    /// Calcola una mossa di emergenza basata sulla legalità immediata se la ricerca fallisce.
    /// </summary>
    byte GetFallbackMove();

    /// <summary>
    /// Resetta lo stato interno dell'engine (root index) tra una partita e l'altra.
    /// </summary>
    void Reset();
}