using Thanos.Enums;
using Thanos.MCST;
using Thanos.Memory;
using Thanos.PreWarm;
using Thanos.SourceGen;

namespace Thanos;

public sealed class BattleSnakeAgent : IDisposable
{ 
    private readonly MemoryPool _pool;
    private readonly MonteCarloEngine _engine;

    
    public BattleSnakeAgent(int maxNodes = Constants.MaxNodes)
    {
        NeighborsBoardCache.Burn(Constants.MaxWidth);
        var neighborsLenght = NeighborsBoardCache.Get(Constants.MaxWidth).Length;
        
        _pool = new MemoryPool(GameContext.Worst(neighborsLenght));
        _engine = new MonteCarloEngine(_pool);
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
        
        _pool.Reset(in context);
    }

    /// <summary>
    /// Chiamato a ogni turno per decidere la mossa.
    /// Implementa la logica di riutilizzo dell'albero basata su hash.
    /// </summary>
    public byte Move(in Request request) => Moves.Up;

    public static void End(in Request request) => Console.WriteLine($"End: {request.Game.Id} - {request.Turn}");
    
    public void Dispose() => _pool.Dispose();
    
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