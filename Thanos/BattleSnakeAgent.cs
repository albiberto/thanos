using Thanos.Common;
using Thanos.MCST;
using Thanos.MCST.Memory;
using Thanos.Memory;
using Thanos.PreWarm;
using Thanos.SourceGen;

namespace Thanos;

public sealed class BattleSnakeAgent : IDisposable
{ 
    private readonly WarMemoryPool _warPool;
    private readonly NodeMemoryPool _nodePool;
    private readonly MonteCarloEngine _engine;

    
    public BattleSnakeAgent(int maxNodes = Constants.MaxNodes)
    {
        NeighborsBoardCache.Burn(Constants.MaxWidth);
        var neighborsLenght = NeighborsBoardCache.Get(Constants.MaxWidth).Length;

        _nodePool = new NodeMemoryPool(NodeMemoryLayout.Standard, maxNodes);
        _warPool = new WarMemoryPool(GameContext.Worst(neighborsLenght), maxNodes);
        _engine = new MonteCarloEngine(_warPool);
    }

    /// <summary>
    /// Chiamato una sola volta all'inizio della partita.
    /// Inizializza il contesto di gioco, la mappa degli ID e il memory pool.
    /// </summary>
    public void Start(in Request request)
    {
        var width = request.Board.Width;
        
        var snakeIdMap = BuildIdMap(request);
        var neighbors = NeighborsBoardCache.Get(width);
        
        var context = new GameContext(width, snakeIdMap, neighbors);
        
        _warPool.Reset(in context);
    }

    /// <summary>
    /// Chiamato a ogni turno per decidere la mossa.
    /// Implementa la logica di riutilizzo dell'albero basata su hash.
    /// </summary>
    public byte Move(in Request request) => Moves.Up;

    public void End(in Request _)
    {
        _warPool.Clear();
    } 
    
    public void Dispose() => _warPool.Dispose();
    
    private static Dictionary<string, int> BuildIdMap(Request request)
    {
        var myId = request.You.Id;

        var snakeIdMap = new Dictionary<string, int>(StringComparer.InvariantCultureIgnoreCase)
        {
            [myId] = 0
        };
        
        foreach (var snake in request.Board.Snakes.Where(s => string.Equals(s.Id, myId, StringComparison.InvariantCultureIgnoreCase))) snakeIdMap[snake.Id] = snakeIdMap.Count;
        
        return snakeIdMap;
    }
}