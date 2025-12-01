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

        _cluster.SetMap(map);
        _cluster.Reset();
    }

    public Task<byte> Move(Request request) => _cluster.ComputeMoveAsync(request);

    public void End(in Request _)
    {
        // Opzionale: logiche di fine partita
    }

    public void Dispose() => _cluster.Dispose();
}