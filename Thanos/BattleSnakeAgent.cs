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
    private Dictionary<Guid, int> _snakeIdMap = new();

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
        _lastChosenIndex = 0;
        
        var width = request.Board.Width;
        var area = request.Board.Area;

        var luts = _lutProvider[area];
        _snakeIdMap = BuildIdMap(in request);

        var layout = new MemoryLayoutBuilder(area, _snakeIdMap.Count).Build();

        _slotPool.Set(in layout, luts, _snakeIdMap, area);
    }

    public byte Move(in Request request)
    {
        // Calcola l'hash dello stato di gioco reale.
        // Viene usato uno slot temporaneo (0) solo per l'hash, non per l'albero.
        var realBoardArena = _slotPool.GetArena(0); 
        realBoardArena.InitializeFromRequest(in request);
        
        // Per un hashing stabile, utilizza la mappa degli ID dei serpenti.
        var realHash = ZobristHasher.CalculateHash(in realBoardArena);
        
        // 1. Tenta di riutilizzare l'albero del turno precedente.
        // Il metodo PrepareNextTurn dell'Engine si occuperà di trovare il nodo corretto
        // nell'albero basato sull'hash o di resettare se non lo trova.
        var isTreeReused = _engine.PrepareNextTurn(_lastChosenIndex, realHash);

        // Usa il valore di ritorno per il log di debug
        Console.WriteLine(isTreeReused 
            ? "[MCTS] Cache HIT! Albero riutilizzato per il turno corrente." 
            : "[MCTS] Cache MISS! Albero resettato.");


        // 2. Lancia la ricerca MCTS dalla radice corretta (quella riutilizzata o una nuova).
        var bestIndex = _engine.FindBestMove(in request);
    
        // Gestisce il caso in cui nessuna mossa valida è stata trovata.
        if (bestIndex == -1)
        {
            // Questo è un fallback critico. Se l'MCTS non ha trovato un nodo valido
            // (es. perché tutte le mosse portano a morte istantanea),
            // è necessario restituire una mossa di emergenza.
            throw new InvalidOperationException("Nessuna mossa valida trovata dall'MCTS.");
        }
    
        // 3. Salva l'indice del nodo scelto per poterlo riutilizzare nel prossimo turno.
        _lastChosenIndex = bestIndex; 
    
        // 4. Recupera la mossa dal nodo migliore e la restituisce.
        ref var chosenNode = ref _nodePool[bestIndex];
        var move = chosenNode.Move;
        
        return move;
    }

    public void End(in Request _)
    {
    }

    private static Dictionary<Guid, int> BuildIdMap(in Request request)
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