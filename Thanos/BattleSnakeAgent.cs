using System;
using Thanos.Enums;
using Thanos.MCST;
using Thanos.Memory;
using Thanos.SourceGen;
using Thanos.War;

namespace Thanos;

public sealed class BattleSnakeAgent : IDisposable
{
    private readonly MemoryPool _pool;
    private readonly MonteCarloEngine _engine;

    public BattleSnakeAgent(int maxNodes = Constants.MaxNodes)
    {
        var worstContext = WarContext.Worst;
        var worstLayout = new MemoryLayout(worstContext, maxNodes);
        
        _pool = new MemoryPool(worstContext, worstLayout);
        
        // CORREZIONE: L'Engine ora dipende solo dal Pool.
        _engine = new MonteCarloEngine(_pool);
    }

    public void Start(in Request request)
    {
        var context = new WarContext(in request);
        var layout = new MemoryLayout(context, Constants.MaxNodes);
        
        // Riconfigura solo il Pool. L'Engine non ne ha bisogno.
        _pool.Reset(context, layout);
        
        _engine.Reset(in request);
    }
    
    public string Move(in Request request)
    {
        _engine.Reset(in request);
        
        var bestMoveByte = _engine.FindBestMove();
        
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
            _ => "up" // Fallback
        };
}