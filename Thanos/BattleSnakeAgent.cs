using Thanos.Common;
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
        _engine = new MonteCarloEngine(_warPool, _nodePool);
    }
    
    public void Start(in Request request)
    {
        Console.WriteLine($"Board: {request.Board.Width}x{request.Board.Height}");
        var width = request.Board.Width;
        
        var snakeIdMap = BuildIdMap(request);
        var neighbors = NeighborsBoardCache.Get(width);
        
        var context = new GameContext(width, snakeIdMap, neighbors);
        var luts = _lutProvider.Get(width);
        _warPool.Reset(in context, in luts);
        _nodePool.Reset();
        _engine.Reset();
    }
    
    public byte Move(in Request request)
    {
        Console.WriteLine($"Turn {request.Turn}, Head: ({request.You.Head.X}, {request.You.Head.Y}), Length: {request.You.Length}, Health: {request.You.Health}");
        // 2. A ogni mossa, resetta SOLO il pool degli stati di simulazione
        _warPool.Clear(); 
        // NON TOCCARE _nodePool.Reset() QUI!

        // 3. L'engine ci dà l'INDICE del nodo che rappresenta la nostra mossa migliore
        var bestNodeIndex = _engine.FindBestMove(in request);

        if (bestNodeIndex == -1) return Moves.Up; // Fallback se non troviamo un nodo valido

        // 4. Se abbiamo trovato un nodo valido...
        ref var chosenNode = ref _nodePool[bestNodeIndex];
        var move = chosenNode.MoveThatLedToThisNode;
            
        // ...diciamo all'engine di promuovere questo nodo a nuova radice per il prossimo turno...
        _engine.SetNewRoot(bestNodeIndex);
            
        Console.WriteLine($"Chosen Move: {move} (Node Index: {bestNodeIndex}, Visits: {chosenNode.Visits}, Wins: {chosenNode.Wins})");
        
        // ...e restituiamo la mossa al server.
        return move;
    }

    public void End(in Request _)
    {
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