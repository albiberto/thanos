using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Thanos.Enums;
using Thanos.SourceGen;

namespace Thanos.War;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WarArena : IDisposable
{
    private readonly byte* _snakesMemory;
    private readonly uint _activeSnakes;
    private readonly uint _snakeStride;

    private fixed long _snakePointers[Constants.MaxSnakes];

    public WarArena(in Request request)
    {
        ref readonly var board = ref request.Board;
        var width = board.Width;

        _activeSnakes = (uint)board.Snakes.Length;
        var capacity = board.Capacity;
        _snakeStride = (uint)(WarSnake.HeaderSize + capacity * sizeof(ushort));

        var snakesMemorySize = _snakeStride * _activeSnakes;
        _snakesMemory = (byte*)NativeMemory.AlignedAlloc(snakesMemorySize, Constants.CacheLineSize);
        
        fixed (long* pointersPtr = _snakePointers) InitializeSnakes(pointersPtr, _snakesMemory, _snakeStride, in request.You, board.Snakes, capacity, width);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void InitializeSnakes(long* pointers, byte* memory, uint stride, in Snake meData, ReadOnlySpan<Snake> snakesData, int capacity, int width)
    {
        // Inizializza il tuo serpente a indice 0
        InitializeSingleSnake(pointers, memory, stride, in meData, 0, capacity, width);

        // Inizializza gli avversari a partire da indice 1
        byte opponentPointerIndex = 1;
        foreach (ref readonly var snakeData in snakesData)
        {
            if (snakeData.Id == meData.Id) continue;
            
            InitializeSingleSnake(pointers, memory, stride, in snakeData, opponentPointerIndex, capacity, width);
            opponentPointerIndex++;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void InitializeSingleSnake(long* pointers, byte* memory, uint stride, in Snake snakeData, byte pointerIndex, int capacity, int boardWidth)
    {
        var length = Math.Min(snakeData.Length, capacity);

        var snakePtr = memory + pointerIndex * stride;
        pointers[pointerIndex] = (long)snakePtr;

        var body1D = stackalloc ushort[length];
        UnrollSnakeBody(length, body1D, in snakeData, boardWidth);

        WarSnake.PlacementNew((WarSnake*)snakePtr, snakeData.Health, length, capacity, body1D);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void UnrollSnakeBody(int length, ushort* body1D, in Snake snakeData, int boardWidth)
    {
        var sourceBody = snakeData.Body;
        // Cicla all'indietro per memorizzare il serpente in ordine Coda -> Testa, come richiesto dalla logica a coda circolare del metodo Move().
        for (var i = 0; i < length; i++) body1D[i] = To1D(in sourceBody[length - 1 - i], boardWidth);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ushort To1D(in Coordinate coord, int width) => (ushort)(coord.Y * width + coord.X);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref WarSnake GetMySnake() => ref *(WarSnake*)_snakePointers[0];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref WarSnake GetSnake(int index) => ref *(WarSnake*)_snakePointers[index];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        if (_snakesMemory != null) NativeMemory.AlignedFree(_snakesMemory);
    }
}