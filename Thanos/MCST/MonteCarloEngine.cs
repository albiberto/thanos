using System.Diagnostics;
using Thanos.Enums;
using Thanos.Math;
using Thanos.Memory;
using Thanos.SourceGen;
using Thanos.War;

namespace Thanos.MCST;

public sealed unsafe class MonteCarloEngine(MemoryPool pool)
{
    public void Reset(in Request request)
    {
    }
    
    public void Reset(in Request request, in WarContext context, in MemoryLayout layout)
    {
    }
}