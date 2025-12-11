using Thanos.SourceGen;

namespace Thanos.Abstract;

public interface IBattleSnakeCluster : IDisposable
{
    void InitializeGame(string[] sortedSnakeIds);
    Task<byte> ComputeMoveAsync(Request request);
    void Reset();
}