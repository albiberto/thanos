using Thanos.Memory;
using Thanos.War;

namespace Thanos.MCST;

public sealed unsafe class MonteCarloEngine(MemoryPool pool, in WarContext context)
{
    private readonly MemoryPool _pool = pool;
    private readonly WarContext _context = context;
    
    public void GetBestMove() => throw new NotImplementedException("Implementa la logica di simulazione per il motore Monte Carlo.");
}