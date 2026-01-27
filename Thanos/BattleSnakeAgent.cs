using Thanos.Abstract;
using Thanos.Common;
using Thanos.Extensions;
using Thanos.MCST;
using Thanos.Memory;
using Thanos.SourceGen;

namespace Thanos;

public sealed class BattleSnakeAgent : IBattleSnakeAgent
{
    private readonly Engine _engine;
    
    // Risorse condivise (Owned by Agent -> Disposed by Agent)
    private readonly ISlotMemoryPool _sharedSlotPool;
    private readonly INodeMemoryPool _sharedNodePool;
    private readonly LookupsMemoryPool _sharedLookups;
    
    // Pool isolato per il calcolo dell'Hash
    private readonly ISlotMemoryPool _hashCalculationPool;

    // Stato sessione
    private int _lastChosenIndex = -1;
    private readonly string[] _idBuffer = new string[Constants.MaxSnakesCount];
    private string[] _activeIds = [];

    public BattleSnakeAgent(
        Engine engine, 
        ISlotMemoryPool sharedSlotPool, 
        INodeMemoryPool sharedNodePool, 
        LookupsMemoryPool sharedLookups)
    {
        _engine = engine;
        _sharedSlotPool = sharedSlotPool;
        _sharedNodePool = sharedNodePool;
        _sharedLookups = sharedLookups;

        // Inizializza il pool per l'hashing (piccolo, solo per la richiesta corrente)
        var slotLayout = new SlotMemoryLayout(Constants.Medium.Area, 64, Constants.MaxSnakesCount);
        _hashCalculationPool = new SlotMemoryPool(5, 0, Constants.MaxSnakesCount, sharedLookups, slotLayout);
    }

    public void Start(in Request request)
    {
        // 1. MAPPING ID: Eroe sempre all'indice 0
        var myId = request.You.Id;
        _idBuffer[0] = myId;

        var enemiesCount = 0;
        foreach (var snake in request.Board.Snakes)
        {
            if (string.Equals(snake.Id, myId, StringComparison.Ordinal)) continue;
            if (1 + enemiesCount >= _idBuffer.Length) break;

            _idBuffer[1 + enemiesCount] = snake.Id;
            enemiesCount++;
        }

        var totalCount = 1 + enemiesCount;
        
        // Ottimizzazione allocazione array ID
        if (_activeIds.Length != totalCount) _activeIds = new string[totalCount];
        Array.Copy(_idBuffer, _activeIds, totalCount);

        // 2. INIZIALIZZAZIONE ENGINE
        _lastChosenIndex = -1;
        _engine.InitializeGame(_activeIds);
    }

    public Task<byte> Move(Request request)
    {
        // 1. Calcolo Hash (Su Pool ISOLATO)
        _hashCalculationPool.Reset(); 
        var tempIndex = _hashCalculationPool.Allocate();
        
        if (tempIndex == -1) return Task.FromResult(_engine.GetFallbackMove()); 

        var targetHash = _hashCalculationPool.CalculateRequestHash(tempIndex, in request, _activeIds);

        // 2. Esecuzione Engine (MCTS Parallelo)
        var bestNodeIndex = _engine.FindBestMove(in request, _lastChosenIndex, targetHash);
        
        _lastChosenIndex = bestNodeIndex;

        // 3. Estrazione Mossa
        if (bestNodeIndex <= 0) 
        {
            return Task.FromResult(_engine.GetFallbackMove());
        }

        ref var node = ref _sharedNodePool.Get(bestNodeIndex);
        return Task.FromResult(node.Move);
    }

    public void End(in Request _)
    {
        // Reset opzionale, gestito già da Start
    }

    public void Dispose()
    {
        _sharedSlotPool.Dispose();
        _sharedNodePool.Dispose();
        _hashCalculationPool.Dispose();
    }
}