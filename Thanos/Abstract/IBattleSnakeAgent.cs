using Thanos.SourceGen;

namespace Thanos.Abstract;

public interface IBattleSnakeAgent : IDisposable
{
    void Start(in Request request);
    Task<byte> Move(Request request);
    void End(in Request request);
}