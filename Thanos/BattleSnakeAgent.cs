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
    private Dictionary<string, int> _snakeIdMap = new();

    public BattleSnakeAgent(uint maxNodes = Constants.MaxNodes)
    {
        _lutProvider = LutProvider.Instance;

        _nodePool = new NodeMemoryPool(maxNodes, NodeMemoryLayout.Default);
        _slotPool = new SlotMemoryPool(maxNodes, MemoryLayoutBuilder.Worst);

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
        Console.WriteLine("\n================ NEW GAME STARTING ================");
        _lastChosenIndex = 0;
        
        var width = request.Board.Width;
        var area = request.Board.Area;
        Console.WriteLine($"[Agent.Start] Game started on a {width}x{request.Board.Height} board (Area: {area}).");


        var luts = _lutProvider[area];
        _snakeIdMap = BuildIdMap(in request);

        var layout = new MemoryLayoutBuilder(area, _snakeIdMap.Count).Build();

        _slotPool.Set(in layout, luts, _snakeIdMap, area);
    }

    public byte Move(in Request request)
    {
        Console.WriteLine($"\n--- Turn {request.Turn} ---");
        // Calcola l'hash dello stato di gioco reale.
        // Viene usato uno slot temporaneo (0) solo per l'hash, non per l'albero.
        var realBoardArena = _slotPool.GetArena(0); 
        realBoardArena.InitializeFromRequest(in request);
        
        // Per un hashing stabile, utilizza la mappa degli ID dei serpenti.
        var realHash = ZobristHasher.CalculateHash(in realBoardArena);
        Console.WriteLine($"[Agent.Move] Current board hash: {realHash}");
        
        // 1. Tenta di riutilizzare l'albero del turno precedente.
        var isTreeReused = _engine.PrepareNextTurn(_lastChosenIndex, realHash);

        // LOGGING: Usa il log già presente in Engine, ma se vuoi puoi decommentarlo anche qui
        // Console.WriteLine(isTreeReused 
        //     ? "[Agent.Move] MCTS tree successfully reused." 
        //     : "[Agent.Move] MCTS tree could not be reused.");


        // 2. Lancia la ricerca MCTS dalla radice corretta.
        var bestIndex = _engine.FindBestMove(in request);
    
        // Gestisce il caso in cui nessuna mossa valida è stata trovata.
        if (bestIndex == -1)
        {
            Console.WriteLine("[Agent.Move] CRITICAL: Engine returned no valid moves. Throwing exception.");
            throw new InvalidOperationException("Nessuna mossa valida trovata dall'MCTS.");
        }
    
        // 3. Salva l'indice del nodo scelto per il prossimo turno.
        _lastChosenIndex = bestIndex; 
    
        // 4. Recupera la mossa dal nodo migliore e la restituisce.
        ref var chosenNode = ref _nodePool[bestIndex];
        var move = chosenNode.Move;
        
        return move;
    }

    public void End(in Request request)
    {
         Console.WriteLine($"================ GAME ENDED AT TURN {request.Turn} ================\n");
    }

    private static Dictionary<string, int> BuildIdMap(in Request request)
    {
        var myId = request.You.Id;

        var snakeIdMap = new Dictionary<string, int>
        {
            [myId] = 0
        };

        foreach (var snake in request.Board.Snakes.Where(s => s.Id != myId)) snakeIdMap[snake.Id] = snakeIdMap.Count;

        // LOGGING
        Console.WriteLine("[Agent.BuildIdMap] Snake ID to Index mapping created:");
        foreach(var entry in snakeIdMap)
        {
            Console.WriteLine($"  -> ID: {entry.Key} => Index: {entry.Value}");
        }

        return snakeIdMap;
    }
}