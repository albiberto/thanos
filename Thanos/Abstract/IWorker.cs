using Thanos.SourceGen;

namespace Thanos.Abstract;

public interface IWorker
{
    void RunIteration(int area, int rootIndex);
    void Reset(RulesetSettings settings);
}