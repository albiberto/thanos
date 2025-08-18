using System;
using Thanos.Enums;
using Thanos.MCST;
using Thanos.Memory;
using Thanos.SourceGen;
using Thanos.War;

namespace Thanos;

public sealed class BattleSnakeAgent : IDisposable
{
    private readonly MemoryPool _pool;
    private readonly MonteCarloEngine _engine;
    private readonly Dictionary<string, int> _snakeIdMap = new();
    
    private unsafe Node* _root;
    private byte _lastChosenMove; // Memorizza l'ultima mossa fatta

    public BattleSnakeAgent(int maxNodes = Constants.MaxNodes)
    {
        var worstLayout = MemoryLayout.Worst;
        
        _pool = new MemoryPool(worstLayout, worstLayout.SlotSize * maxNodes);
        _engine = new MonteCarloEngine(_pool);
    }

    public unsafe void Start(in Request request)
    {
        foreach (var snake in request.Board.Snakes)
        {
            if (!_snakeIdMap.ContainsKey(snake.Id))
            {
                _snakeIdMap[snake.Id] = _nextSnakeIntId++;
            }
        }
        
        var layout = new MemoryLayout(request.Board.Area, request.Board.SnakeCount);
        _pool.Reset(layout);
        
        if (_pool.TryGetNext(out var rootSlot))
        {
            rootSlot.InitializeFromRequest(in request);
            _root = rootSlot.GetNodePtr();
        }
    }

    public unsafe byte Move(in Request request)
    {
        // --- LOGICA DI RIUTILIZZO DELL'ALBERO ---
        // Cerca nell'albero precedente il figlio che corrisponde alla nostra ultima mossa
        var newRoot = _root->FindChildByMove(_lastChosenMove);

        // Se non lo troviamo (o è il primo turno), resettiamo partendo dallo stato attuale
        if (newRoot == null)
        {
            if (_pool.TryGetNext(out var rootSlot))
            {
                rootSlot.InitializeFromRequest(in request);
                newRoot = rootSlot.GetNodePtr();
            }
        }
        _root = newRoot;
        _root->Parent = null; // Il nuovo_root non ha più un genitore

        var bestMoveByte = _engine.FindBestMove(_root, in request);
        
        // Memorizza la mossa che stiamo per fare per il prossimo turno
        _lastChosenMove = bestMoveByte;
        
        return bestMoveByte;
    }

    public static void End(in Request request) => Console.WriteLine($"End: {request.Game.Id} - {request.Turn}");
    
    public void Dispose() => _pool.Dispose();


}