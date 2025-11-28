using Thanos.Common;
using Thanos.MCST;
using Thanos.SourceGen;

namespace Thanos;

public sealed class BattleSnakeAgent(uint maxNodes = Constants.MaxNodes) : IDisposable
{
    private readonly EngineCluster _cluster = new(maxNodes);

    public void Start(in Request request)
    {
        var myId = request.You.Id;

        var map = new Dictionary<string, int>
        {
            [myId] = 0
        };

        foreach (var snake in request.Board.Snakes.Where(s => s.Id != myId)) map[snake.Id] = map.Count;
        
#if DEBUG
        Console.WriteLine($"[BattleSnakeAgent.Start] Assigned IDs: {string.Join(", ", map.Select(kv => $"{kv.Key}:{kv.Value}"))}");
#endif

        // Passiamo la mappa al cluster che la propaga a tutti i pool
        _cluster.SetMap(map);
        _cluster.Reset();
    }

    public Task<byte> Move(Request request) // Rimuovi 'in' se async dà problemi con ref struct, ma Request è readonly struct normale
    { 
        return _cluster.ComputeMoveAsync(request); 
    }

    public void End(in Request _)
    {
        // Opzionale: logiche di fine partita
    }

    public void Dispose()
    {
        _cluster.Dispose();
    }
}