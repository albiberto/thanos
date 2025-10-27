using System.Text;
using Thanos.Common;
using Thanos.MCST;
using Thanos.Memory;
using Thanos.SourceGen;

namespace Thanos;

public sealed class BattleSnakeAgent : IDisposable
{
    private readonly Engine _engine;

    private readonly NodeMemoryPool _nodePool;
    private readonly SlotMemoryPool _slotPool;

    private int _lastChosenIndex;

    public BattleSnakeAgent(uint maxNodes = Constants.MaxNodes)
    {
        _nodePool = new NodeMemoryPool(maxNodes, NodeMemoryLayout.Default);
        _slotPool = new SlotMemoryPool(maxNodes, new LookupsMemoryPool(LookupsMemoryLayout.Medium), SlotMemoryLayout.Worst);

        _engine = new Engine(_slotPool, _nodePool);
    }

    public void Start(in Request request)
    {
        _lastChosenIndex = 0;

        var map = BuildSnakeMap(in request);

        _slotPool.Set(map);
    }

    public byte Move(in Request request)
    {
        _engine.PrepareNextTurn(_lastChosenIndex);
        
        var bestIndex = _engine.FindBestMove(in request);
    
        if (bestIndex == -1) return Moves.None;
    
        _lastChosenIndex = bestIndex; 
    
        ref var chosenNode = ref _nodePool[_lastChosenIndex];
        var move = chosenNode.Move;
        
        return move;
    }
    
    public void End(in Request request) { }

    private static Dictionary<string, int> BuildSnakeMap(in Request request)
    {
        var myId = request.You.Id;

        var map = new Dictionary<string, int>
        {
            [myId] = 0
        };

        foreach (var snake in request.Board.Snakes.Where(s => s.Id != myId)) map[snake.Id] = map.Count;
        
        return map;
    }
    
    public void Dispose()
    {
        _slotPool.Dispose();
        _nodePool.Dispose();
    }
}