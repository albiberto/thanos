using Thanos.Enums;
using Thanos.MCST;
using Thanos.Memory;
using Thanos.SourceGen;
using Thanos.War;

namespace Thanos;

public sealed class BattleSnakeAgent : IDisposable
{
    // I campi ora sono readonly perché inizializzati una sola volta nel costruttore.
    private readonly MemoryPool _pool;
    private readonly MonteCarloEngine _engine;

    /// <summary>
    /// Costruttore (Bootstrap): Alloca tutta la memoria necessaria per il caso peggiore.
    /// </summary>
    public BattleSnakeAgent(int maxNodes = Constants.MaxNodes)
    {
        // 1. Calcola il layout per il caso peggiore possibile per allocare abbastanza memoria.
        var worstContext = WarContext.Worst; // Un contesto con il massimo numero di serpenti, area massima, etc.
        var worstLayout = new MemoryLayout(worstContext, maxNodes);
        
        // 2. Crea il Pool e l'Engine una sola volta.
        _pool = new MemoryPool(worstContext, worstLayout);
        _engine = new MonteCarloEngine(_pool, worstContext, worstLayout);
    }

    /// <summary>
    /// Chiamato all'inizio di una partita. Riconfigura le strutture esistenti.
    /// </summary>
    public void Start(in Request request)
    {
        // 1. Calcola il contesto e il layout specifici per QUESTA partita.
        var context = new WarContext(in request.Board);
        var layout = new MemoryLayout(context, Constants.MaxNodes);
        
        // 2. Riconfigura il Pool e l'Engine con i nuovi parametri.
        _pool.Reset(context, layout);
        _engine.Reset(context, layout);
        
        // 3. Imposta lo stato iniziale dell'albero per il turno 0.
        _engine.Reset(in request);
    }
    
    /// <summary>
    /// Chiamato a ogni turno per decidere la mossa.
    /// </summary>
    public string Move(in Request request)
    {
        // 1. Resetta l'albero di ricerca allo stato del turno corrente.
        _engine.Reset(in request);
        
        // 2. Esegui la ricerca MCTS.
        //    Il numero di iterazioni dipende dal tempo concesso dall'API di BattleSnake (es. < 500ms).
        byte bestMoveByte = _engine.FindBestMove(iterations: 50000);
        
        // 3. Traduci il risultato nel formato stringa richiesto dall'API.
        return ToApiMove(bestMoveByte);
    }

    public void End(in Request request) => Console.WriteLine($"End: {request.Game.Id} - {request.Turn}");
    
    public void Dispose() => _pool.Dispose();

    private static string ToApiMove(byte move) =>
        move switch
        {
            Moves.Up => "up",
            Moves.Down => "down",
            Moves.Left => "left",
            Moves.Right => "right",
            _ => "up"
        };
}