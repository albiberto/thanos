using Thanos.Shared;

namespace Thanos.Abstract;

public interface ILookupsMemoryPool : IDisposable
{
    CoordinatesMatrix CoordinatesMatrix { get; }
    NeighborsMatrix NeighborsMatrix { get; }
}