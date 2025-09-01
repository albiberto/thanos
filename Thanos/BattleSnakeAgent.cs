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


        var width = request.Board.Width;

        var snakeIdMap = BuildIdMap(request);

        var neighbors = NeighborsBoardCache.Get(width);

        var context = new GameContext(width, snakeIdMap, neighbors);

        var luts = _lutProvider.Get(width);
        _warPool.Set(in context, in luts);
        _nodePool.Reset();
        _engine.Reset();
    }

    public byte Move(in Request request)
    {
        // 1. A ogni mossa, resetta SOLO il pool degli stati di simulazione
        _warPool.Reset();

        // 2. All'inizio del turno, prova ad aggiornare la radice dell'albero
        _engine.PrepareNextTurn(_lastChosenNodeIndex, in request, BuildIdMap(request));

        // 3. Ora lancia la ricerca dalla radice corretta (o una nuova se c'è stato un reset)
        var bestNodeIndex = _engine.FindBestMove(in request);

        if (bestNodeIndex != -1)
        {
            ref var chosenNode = ref _nodePool[bestNodeIndex];

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
        // LOG: Stampa lo stato finale dell'albero dell'ultima mossa
        Console.WriteLine("===== Fine Partita: Stato Finale dell'Albero =====");
        // Dovrai accedere al _nodePool e all'indice della radice dell'ultimo turno
        TreeLogger.LogTreeState(_nodePool, _engine._currentRootIndex);
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

public static class TreeLogger
{
    public static void LogTreeState(NodeMemoryPool nodePool, int rootNodeIndex)
    {
        Console.WriteLine("\n--- Stato Albero di Ricerca ---");
        if (rootNodeIndex < 0 || rootNodeIndex >= nodePool._offset)
        {
            Console.WriteLine("Radice non valida.");
            return;
        }
        PrintNodeRecursive(rootNodeIndex, nodePool, "", true);
        Console.WriteLine("--- Fine Stato Albero ---\n");
    }

    private static void PrintNodeRecursive(int nodeIndex, NodeMemoryPool nodePool, string indent, bool isLast)
    {
        ref var node = ref nodePool[nodeIndex];
        
        Console.Write(indent);
        if (isLast)
        {
            Console.Write("└── ");
            indent += "    ";
        }
        else
        {
            Console.Write("├── ");
            indent += "|   ";
        }

        Console.WriteLine($"Nodo {nodeIndex}: Move={MoveToString(node.MoveThatLedToThisNode)}, " +
                          $"Parent={node.ParentIndex}, Child={node.FirstChildIndex}, Sibling={node.NextSiblingIndex}");

        // Itera sui figli usando la struttura a lista concatenata
        var childIndex = node.FirstChildIndex;
        while (childIndex != -1)
        {
            ref var childNode = ref nodePool[childIndex];
            var isLastChild = childNode.NextSiblingIndex == -1;
            PrintNodeRecursive(childIndex, nodePool, indent, isLastChild);
            childIndex = childNode.NextSiblingIndex;
        }
    }
    
    // Includi anche questo helper se non l'hai già messo altrove
    private static string MoveToString(byte move) => move switch
    {
        Moves.Up => "Up",
        Moves.Down => "Down",
        Moves.Left => "Left",
        Moves.Right => "Right",
        _ => "Root/None" // È utile per il nodo radice
    };
}