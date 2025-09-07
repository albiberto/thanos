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
    private readonly Engine _engine;

    private readonly LutProvider _lutProvider;
    private readonly NodeMemoryPool _nodePool;
    private readonly SlotMemoryPool _slotPool;

    private int _lastChosenIndex;

    public BattleSnakeAgent(int maxNodes = Constants.MaxNodes)
    {
        NeighborsBoardCache.Burn(Constants.MaxWidth);
        var neighbors = NeighborsBoardCache.Get(Constants.MaxWidth);

        var layout = new MemoryLayoutBuilder(Constants.MaxArea, Constants.MaxSnakesCount).Build();
        
        _nodePool = new NodeMemoryPool(NodeMemoryLayout.Instance, maxNodes);
        _lutProvider = new LutProvider(Constants.MaxWidth, Constants.MaxArea);
        _slotPool = new SlotMemoryPool(Constants.MaxNodes, layout, neighbors, Constants.MaxArea, new Dictionary<Guid, int>());
        _engine = new Engine(_slotPool, _nodePool);
    }

    public void Dispose()
    {
        _lutProvider.Dispose();
        _slotPool.Dispose();
        _nodePool.Dispose();
    }

    public void Start(in Request request)
    {
        _lastChosenIndex = 0; // Resetta a inizio partita
        var width = request.Board.Width;
        var area = request.Board.Area;
        
        var luts = _lutProvider.Get(width);
        _engine.Reset(luts);
        
        var snakeIdMap = BuildIdMap(request);
        var neighbors = NeighborsBoardCache.Get(width);
        var layout = new MemoryLayoutBuilder(Constants.MaxArea, Constants.MaxSnakesCount).Build();
        
        _slotPool.Set(layout, neighbors, area, snakeIdMap);
    }

    public byte Move(in Request request)
    {
        // 2. All'inizio del turno, prova ad aggiornare la radice dell'albero
        // _engine.PrepareNextTurn(_lastChosenIndex, in request, BuildIdMap(request));

        // 3. Ora lancia la ricerca dalla radice corretta (o una nuova se c'è stato un reset)
        var bestIndex = _engine.FindBestMove(in request);

        if (bestIndex != -1)
        {
            ref var chosenNode = ref _nodePool[bestIndex];

            var move = chosenNode.Move;

            _lastChosenIndex = bestIndex; // Salva la scelta per il prossimo turno

            // Log e return
            return move;
        }

        // Fallback
        throw new InvalidOperationException("No valid move found");
    }

    public void End(in Request _)
    {
    }

    private static Dictionary<Guid, int> BuildIdMap(Request request)
    {
        var myId = request.You.Id;

        var snakeIdMap = new Dictionary<Guid, int>
        {
            [myId] = 0
        };

        foreach (var snake in request.Board.Snakes.Where(s => s.Id != myId)) snakeIdMap[snake.Id] = snakeIdMap.Count;

        return snakeIdMap;
    }
}