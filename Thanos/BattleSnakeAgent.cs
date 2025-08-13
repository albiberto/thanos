using System.Runtime.InteropServices;
using Thanos.Enums;
using Thanos.MCST;
using Thanos.Memory;
using Thanos.SourceGen;
using Thanos.War;

namespace Thanos;

public sealed unsafe class BattleSnakeAgent : IDisposable
{
    private readonly byte* _memoryPtr;

    private WarContext _context;
    private MemoryLayout _layout;
    
    private readonly MemoryPool _pool;
    private readonly MonteCarloEngine _engine;

    public BattleSnakeAgent(uint maxNodes = Constants.MaxNodes)
    {
        var worstContext = WarContext.Worst;
        var worstLayout = new MemoryLayout(worstContext, maxNodes);
        
        _memoryPtr =  (byte*)NativeMemory.AlignedAlloc(worstLayout.Sizes.Pool, Constants.SizeOfCacheLine);
        
        _pool = new MemoryPool(_memoryPtr);
        _engine = new MonteCarloEngine(_pool);
    }

    public void Start(in Request request)
    {
        _context = new WarContext(in request.Board);
        _layout = new MemoryLayout(_context, Constants.MaxNodes);
        
        _pool.Reset(_context, _layout);
        _engine.Reset(in request, _context, _layout);
    }
    
    public MoveDirection Move(in Request request)
    {
        _pool.Reset();
        _engine.Reset(in request);
        
        return MoveDirection.Down;
    }

    public void End(in Request request) => Console.WriteLine($"End: {request.Game.Id} - {request.Turn}");

    public void Dispose() => NativeMemory.AlignedFree(_memoryPtr);
}