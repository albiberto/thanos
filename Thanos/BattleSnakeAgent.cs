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
    private readonly LookupsMemoryPool _lookupsMemoryPool = new(LookupsMemoryLayout.Medium);

    private int _lastChosenIndex;

    public BattleSnakeAgent(uint maxNodes = Constants.MaxNodes)
    {
        _nodePool = new NodeMemoryPool(maxNodes, NodeMemoryLayout.Default);
        _slotPool = new SlotMemoryPool(maxNodes, _lookupsMemoryPool, SlotMemoryLayout.Worst);

        _engine = new Engine(_slotPool, _nodePool);
    }

    public void Start(in Request request)
    {
        _lastChosenIndex = 0;

        var myId = request.You.Id;

        var map = new Dictionary<string, int>
        {
            [myId] = 0
        };

        foreach (var snake in request.Board.Snakes.Where(s => s.Id != myId)) map[snake.Id] = map.Count;
        
        #if DEBUG
        Console.WriteLine($"[BattleSnakeAgent.Start] Assigned IDs: {string.Join("\n ", map.Select(kv => $"Snake-{kv.Key}: {kv.Value}"))}");
        #endif

        _slotPool.Set(map);
        _engine.Reset();
    }

    public byte Move(in Request request)
    {
        var bestIndex = _engine.FindBestMove(in request, _lastChosenIndex);

        if (bestIndex == -1) return Moves.None;

        _lastChosenIndex = bestIndex;

        ref var chosenNode = ref _nodePool[_lastChosenIndex];
        var move = chosenNode.Move;

        return move;
    }

    public void End(in Request request)
    {
    }

    public void Dispose()
    {
        _slotPool.Dispose();
        _nodePool.Dispose();
    }
}