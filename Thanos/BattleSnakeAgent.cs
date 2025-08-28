using Thanos.MCST;
using Thanos.MCST.Memory;
using Thanos.Memory;
using Thanos.PreWarm;
using Thanos.PreWarm.Memory;
using Thanos.SourceGen;

namespace Thanos;

public sealed class BattleSnakeAgent : IDisposable
{ 
    private readonly WarMemoryPool _warPool;
    private readonly NodeMemoryPool _nodePool;
    
    private readonly LutProvider _lutProvider;
    private readonly MonteCarloEngine _engine;
    
    public BattleSnakeAgent(int maxNodes = Constants.MaxNodes)
    {
        NeighborsBoardCache.Burn(Constants.MaxWidth);
        var neighborsLenght = NeighborsBoardCache.Get(Constants.MaxWidth).Length;

        _nodePool = new NodeMemoryPool(NodeMemoryLayout.Standard, maxNodes);
        _lutProvider = new LutProvider(Constants.MaxWidth, Constants.MaxArea);
        _warPool = new WarMemoryPool(GameContext.Worst(neighborsLenght), maxNodes);
        _engine = new MonteCarloEngine(_warPool, _nodePool, _lutProvider);
    }
    
    public void Start(in Request request)
    {
        var width = request.Board.Width;
        
        var snakeIdMap = BuildIdMap(request);
        var neighbors = NeighborsBoardCache.Get(width);
        
        var context = new GameContext(width, snakeIdMap, neighbors);
        var luts = _lutProvider.Get(width);
        
        _warPool.Reset(in context, in luts);
        _nodePool.Reset();
    }
    
    public byte Move(in Request request)
    {
        _nodePool.Reset();
        return _engine.FindBestMove(in request);
    }

    public void End(in Request _)
    {
        _warPool.Clear();
        _nodePool.Clear();
    } 
    
    public void Dispose()
    {
        _lutProvider.Dispose();
        _warPool.Dispose();
        _nodePool.Dispose();
    }

    private static Dictionary<string, int> BuildIdMap(Request request)
    {
        var myId = request.You.Id;

        var snakeIdMap = new Dictionary<string, int>(StringComparer.InvariantCultureIgnoreCase)
        {
            [myId] = 0
        };
        
        foreach (var snake in request.Board.Snakes.Where(s => !string.Equals(s.Id, myId, StringComparison.InvariantCultureIgnoreCase))) snakeIdMap[snake.Id] = snakeIdMap.Count;
        
        return snakeIdMap;
    }
}