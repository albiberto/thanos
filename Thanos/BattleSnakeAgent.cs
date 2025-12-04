using Thanos.Abstract;
using Thanos.SourceGen;

namespace Thanos;

public sealed class BattleSnakeAgent(IBattleSnakeCluster cluster) : IBattleSnakeAgent, IDisposable
{
    private readonly IBattleSnakeCluster _cluster = cluster ?? throw new ArgumentNullException(nameof(cluster));
    private readonly string[] _idBuffer = new string[Constants.MaxSnakesCount];

    public void Start(in Request request)
    {
        var myId = request.You.Id;

        // 1. Hero always at index 0
        _idBuffer[0] = myId;

        // 2. Select enemies
        var enemies = request.Board.Snakes
            .Where(s => !string.Equals(s.Id, myId, StringComparison.Ordinal))
            .Select(s => s.Id)
            .ToArray();

        // 3. Copy enemies into the main buffer starting from index 1
        if (enemies.Length > 0) Array.Copy(enemies, 0, _idBuffer, 1, enemies.Length);

        // 4. Calculate the total count (1 Hero + N Enemies)
        var totalCount = 1 + enemies.Length;

        _cluster.InitializeGame(_idBuffer, totalCount);
        _cluster.Reset();
    }

    public Task<byte> Move(Request request) => _cluster.ComputeMoveAsync(request);

    public void End(in Request _)
    {
    }

    public void Dispose() => _cluster.Dispose();
}