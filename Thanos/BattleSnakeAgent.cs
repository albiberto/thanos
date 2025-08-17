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
    
    private WarContext _context;

    public BattleSnakeAgent(int maxNodes = Constants.MaxNodes)
    {
        var worstContext = WarContext.Worst;
        var worstLayout = new MemoryLayout(worstContext, maxNodes);
        
        _pool = new MemoryPool(worstContext, worstLayout);
        _engine = new MonteCarloEngine(_pool);
    }

    public void Start(in Request request)
    {
        _context = new WarContext(in request);
        var layout = new MemoryLayout(_context, Constants.MaxNodes);
        
        _pool.Reset(_context, layout);
        _engine.Reset(in request);
    }
    
    public byte Move(in Request request)
    {
        _engine.Reset(in request);
        return _engine.FindBestMove(_context.Timeout);
    }

    public void End(in Request request) => Console.WriteLine($"End: {request.Game.Id} - {request.Turn}");
    
    public void Dispose() => _pool.Dispose();


}