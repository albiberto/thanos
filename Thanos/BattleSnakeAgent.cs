using System.Text;
using Thanos.Common;
using Thanos.MCST;
using Thanos.MCST.Memory;
using Thanos.Memory;
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

    public BattleSnakeAgent(uint maxNodes = Constants.MaxNodes)
    {
        _lutProvider = LutProvider.Instance;

        _nodePool = new NodeMemoryPool(maxNodes, NodeMemoryLayout.Default);
        _slotPool = new SlotMemoryPool(maxNodes, MemoryLayoutBuilder.Worst);

        _engine = new Engine(_slotPool, _nodePool);
    }

    public void Start(in Request request)
    {
        Console.WriteLine("\n================ NEW GAME STARTING ================");
        Console.WriteLine($"[Agent.Start] Game started on a {request.Board.Width}x{request.Board.Height} board (Area: {request.Board.Area}).");
        
        _engine.Reset();
        _lastChosenIndex = 0;

        var area = request.Board.Area;
        var lookupPtr = _lutProvider[area];
        var map = BuildSnakeMap(in request);
        var layout = new MemoryLayoutBuilder(area, map.Count).Build();

        _slotPool.Set(area, lookupPtr, map, in layout);
    }

    public byte Move(in Request request)
    {
        #if DEBUG
            Console.WriteLine($"\n--- Turn {request.Turn} ---");
        #endif
  
        var hash = CalculateRequestHash(request);
        _engine.PrepareNextTurn(_lastChosenIndex, hash);
        
        var bestIndex = _engine.FindBestMove(in request);
    
        if (bestIndex == -1)
        {
            Console.WriteLine("[Agent.Move] CRITICAL: Engine returned no valid moves.");
            return Moves.None;
        }
    
        _lastChosenIndex = bestIndex; 
    
        ref var chosenNode = ref _nodePool[_lastChosenIndex];
        var move = chosenNode.Move;
        
        return move;
    }
    
    private long CalculateRequestHash(Request request)
    {
        var arena = _slotPool.GetArena(0); 
        arena.InitializeFromRequest(in request);
        var hash = ZobristHasher.CalculateHash(in arena);
        
        #if DEBUG
            Console.WriteLine($"[Agent.Move] Request hash calculated: {hash}");
        #endif
        
        return hash;
    }
    
    public void End(in Request request)
    {
         Console.WriteLine($"================ GAME ENDED AT TURN {request.Turn} ================\n");
         Console.WriteLine();
    }

    private static Dictionary<string, int> BuildSnakeMap(in Request request)
    {
        var myId = request.You.Id;

        var map = new Dictionary<string, int>
        {
            [myId] = 0
        };

        foreach (var snake in request.Board.Snakes.Where(s => s.Id != myId)) map[snake.Id] = map.Count;

        #if DEBUG
            LogMap(map);
        #endif
        
        return map;
    }

    private static void LogMap(Dictionary<string, int> map)
    {
        var sb = new StringBuilder();
        sb.AppendLine("[Agent.BuildIdMap] Snake ID to Index mapping created:");
        foreach (var entry in map) sb.AppendLine($"  -> ID: {entry.Key} => Index: {entry.Value}");
        
        Console.WriteLine(sb.ToString());
    }
    
    public void Dispose()
    {
        _lutProvider.Dispose();
        _slotPool.Dispose();
        _nodePool.Dispose();
    }
}