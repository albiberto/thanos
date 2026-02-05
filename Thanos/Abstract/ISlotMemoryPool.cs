using Thanos.War;
using Thanos.War.Brain;
using Thanos.War.State;

namespace Thanos.Abstract;

public interface ISlotMemoryPool : IDisposable
{
    uint Capacity { get; }
    int Index { get; }
    
    GameState GetGameState(int index);
    Heuristics GetHeuristics(int index);

    int Allocate();
    
    /// <summary>
    /// Alloca un blocco contiguo di slot in modo atomico.
    /// </summary>
    int AllocateBatch(int count);

    void Reset();
}