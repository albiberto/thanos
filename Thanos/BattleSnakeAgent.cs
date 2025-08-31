using System.Text.Json;
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
    
    private int _lastChosenNodeIndex = 0; 
    
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
        _lastChosenNodeIndex = 0; // Resetta a inizio partita
        
        // Console.WriteLine($"Board: {request.Board.Width}x{request.Board.Height}");
        
        var width = request.Board.Width;
        
        var snakeIdMap = BuildIdMap(request);
        // Console.WriteLine($"Snake IDs: {JsonSerializer.Serialize(snakeIdMap)}");
        
        var neighbors = NeighborsBoardCache.Get(width);
        // Console.WriteLine($"Neighbors: {JsonSerializer.Serialize(neighbors)}");
        
        var context = new GameContext(width, snakeIdMap, neighbors);
        // Console.WriteLine($"Context: {JsonSerializer.Serialize(snakeIdMap)}");
        
        var luts = _lutProvider.Get(width);
        _warPool.Set(in context, in luts);
        _nodePool.Reset();
        _engine.Reset();
    }
    
    public byte Move(in Request request)
    {
        // Console.WriteLine($"Turn {request.Turn}, Head: ({request.You.Head.X}, {request.You.Head.Y}), Length: {request.You.Length}, Health: {request.You.Health}");
        
        // 1. A ogni mossa, resetta SOLO il pool degli stati di simulazione
        _warPool.Reset(); 
        
        // 2. All'inizio del turno, prova ad aggiornare la radice dell'albero
        _engine.PrepareNextTurn(_lastChosenNodeIndex, in request, BuildIdMap(request));
        
        // 3. Ora lancia la ricerca dalla radice corretta (o una nuova se c'è stato un reset)
        var bestNodeIndex = _engine.FindBestMove(in request);
        // Console.WriteLine($"Best Node Index: {bestNodeIndex}");
        
        if (bestNodeIndex != -1)
        {
            ref var chosenNode = ref _nodePool[bestNodeIndex];
            // Console.WriteLine($"Node: {JsonSerializer.Serialize(chosenNode)}");
            
            var move = chosenNode.MoveThatLedToThisNode;
            
            _lastChosenNodeIndex = bestNodeIndex; // Salva la scelta per il prossimo turno
            
            // Log e return
            return move;
        }
        
        // Fallback
        _lastChosenNodeIndex = 0; // Resetta per il prossimo turno
        _engine.Reset();
        _nodePool.Reset(); // <-- CORREZIONE: Resetta anche il pool
        return Moves.Up; // O FindQuickSafeMove
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