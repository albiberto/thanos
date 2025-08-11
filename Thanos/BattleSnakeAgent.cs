using System.Numerics;
using System.Runtime.InteropServices;
using Thanos.Enums;
using Thanos.Extensions;
using Thanos.MCST;
using Thanos.Memory;
using Thanos.SourceGen;
using Thanos.War;
using static System.Threading.Tasks.Task;

namespace Thanos;

public sealed unsafe class BattleSnakeAgent : IDisposable
{
    private readonly byte* _memoryPtr;
    private readonly uint _maxNodes;
    
    public BattleSnakeAgent(uint maxNodes = Constants.MaxNodes)
    {
        _maxNodes = maxNodes;
        
        var context = WarContext.Worst;
        var sizes = CalculateSizes(context, _maxNodes);
        
        _memoryPtr =  (byte*)NativeMemory.AlignedAlloc(sizes.SizeOfMemoryPool, Constants.SizeOfCacheLine);
    }

    public Task<MoveDirection> HandleMoveAsync(in Request request)
    {
        var sizes = CalculateSizes(in request.Board, _maxNodes);

        // Calcola gli offset per il primo slot di memoria.
        const uint warContextOffset = 0;
        var memoryLayoutOffset = warContextOffset + sizes.SizeOfContext;

        // Crea le "fette" (Span) di memoria per WarContext e MemoryLayout.
        var warContextSlice = new Span<byte>(_memoryPtr + warContextOffset, (int)sizes.SizeOfContext);
        var memoryLayoutSlice = new Span<byte>(_memoryPtr + memoryLayoutOffset, (int)sizes.SizeOfLayout);

        // Ottieni i riferimenti alle strutture in memoria.
        ref var context = ref MemoryMarshal.AsRef<WarContext>(warContextSlice);
        ref var layout = ref MemoryMarshal.AsRef<MemoryLayout>(memoryLayoutSlice);

        // Qui puoi inizializzare e usare warContext e memoryLayout.
        var memoryPool = new MemoryPool(_memoryPtr + memoryLayoutOffset + sizes.SizeOfLayout, layout, context);
        var engine = new MonteCarloEngine(memoryPool, in context);
    
        engine.GetBestMove();
        
        return FromResult(MoveDirection.Right);
    }

    private static (uint SizeOfContext, uint SizeOfLayout, uint SizeOfArena, uint SizeOfNode, uint SnakeStride, uint SnakeBodyStride, uint SizeOfSnakes, uint BitboardStride, uint SizeOfBitboards, uint SizeOfFiled, uint SizeOfMemorySlot, uint SizeOfMemoryPool) 
        CalculateSizes(in WarContext context, uint maxNodes)
    {
        return CalculateSizes(context.Width, context.Height, context.WarSnakeCount, maxNodes);
    }

    private static (uint SizeOfContext, uint SizeOfLayout, uint SizeOfArena, uint SizeOfNode, uint SnakeStride, uint SnakeBodyStride, uint SizeOfSnakes, uint BitboardStride, uint SizeOfBitboards, uint SizeOfFiled, uint SizeOfMemorySlot, uint SizeOfMemoryPool) 
        CalculateSizes(in Board board, uint maxNodes)
    {
        return CalculateSizes(board.Width, board.Height, board.SnakeCount, maxNodes);
    }

    private static (uint SizeOfContext, uint SizeOfLayout, uint SizeOfArena, uint SizeOfNode, uint SnakeStride, uint SnakeBodyStride, uint SizeOfSnakes, uint BitboardStride, uint SizeOfBitboards, uint SizeOfFiled, uint SizeOfMemorySlot, uint SizeOfMemoryPool) 
        CalculateSizes(uint width, uint height, uint snakeCount, uint maxNodes)
    {
        var area = width * height;
        
        var idealSnakeBodyCapacity = BitOperations.RoundUpToPowerOf2(area);
        var realSnakeBodyCapacity = Math.Min(idealSnakeBodyCapacity, Constants.MaxSnakeBodyCapacity);

        // Calculate the memory of the structures.
        var SizeOfContext = sizeof(WarContext).AlignUp();
        var SizeOfLayout = sizeof(MemoryLayout).AlignUp();
        var SizeOfArena = sizeof(WarArena).AlignUp();
        var SizeOfNode = sizeof(Node).AlignUp();
        
        // Calculate the memory sizes of the snakes.
        var SnakeBodyStride = realSnakeBodyCapacity * sizeof(ushort);
        var SnakeStride = SnakeBodyStride + WarSnake.SizeOfHeader;
        var SizeOfSnakes = (SnakeStride * snakeCount).AlignUp();
        
        // Calculate the memory size of the bitboards.
        var bitboardSegments = (area + 63) >> 6; // (Area + 63) / 64
        var BitboardStride = bitboardSegments * sizeof(ulong);
        var SizeOfBitboards = (BitboardStride * WarField.TotalBitboards).AlignUp();
        var SizeOfFiled = WarField.HeaderSize + SizeOfBitboards;
        
        // Calculate the total memory size of slot and pool.
        var SizeOfMemorySlot = SizeOfContext + SizeOfLayout + SizeOfArena + SizeOfNode + SizeOfSnakes + SizeOfBitboards;
        var SizeOfMemoryPool = SizeOfMemorySlot * maxNodes;
        
        return(SizeOfContext, SizeOfLayout, SizeOfArena, SizeOfNode, SnakeStride, SnakeBodyStride, SizeOfSnakes, BitboardStride, SizeOfBitboards, SizeOfFiled, SizeOfMemorySlot, SizeOfMemoryPool);
    }

    public void Dispose() => NativeMemory.AlignedFree(_memoryPtr);
}