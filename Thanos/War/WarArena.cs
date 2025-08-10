// Thanos/War/WarArena.cs

using System.Numerics;
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
    
    public WarArena(Request request)
    {
        var board = request.Board;
        var width = board.Width;

        _activeSnakes = (uint)board.Snakes.Length;

        var capacity = board.Capacity;

        // --- Step 1: Calcolo Layout di Memoria ---
        _snakeStride = (uint)(WarSnake.HeaderSize + capacity * sizeof(ushort));
        
        // --- Step 2: Allocazione Memoria ---
        var snakesMemorySize = _snakeStride * _activeSnakes;
        _snakesMemory = (byte*)NativeMemory.AlignedAlloc(snakesMemorySize, Constants.CacheLineSize);
        
        // --- Step 3: Inizializzazione dei Serpenti ("Me" a indice 0) ---
        fixed (long* pointersPtr = _snakePointers) InitializeSnakes(pointersPtr, _snakesMemory, _snakeStride, request.You, request.Board.Snakes, capacity, width);
    }
    
    private static void InitializeSnakes(long* pointers, byte* memory, uint stride, Snake meData, ReadOnlySpan<Snake> snakesData, int capacity, int width)
    {
        // **PASSO 1: Inizializza il TUO serpente all'indice 0.**
        InitializeSingleSnake(pointers, memory, stride, meData, 0, capacity, width);

        // **PASSO 2: Inizializza gli avversari.**
        byte opponentPointerIndex = 1; 
        foreach (var snakeData in snakesData)
        {
            if (snakeData.Id == meData.Id) continue;

            InitializeSingleSnake(pointers, memory, stride, snakeData, opponentPointerIndex, capacity, width);
            opponentPointerIndex++;
        }
    }

    private static void InitializeSingleSnake(long* pointers, byte* memory, uint stride, Snake snakeData, byte pointerIndex, int capacity, int boardWidth)
    {
        var lenght = Math.Min(snakeData.Length, capacity);

        // Calcola il puntatore alla memoria per questo serpente usando i parametri.
        var snakePtr = memory + pointerIndex * stride;
        pointers[pointerIndex] = (long)snakePtr;

        // Converte il corpo in 1D sullo stack.
        var body1D = stackalloc ushort[lenght];
        UnrollSnakeBody(lenght, body1D, snakeData, boardWidth);

        // Inizializza la struct WarSnake in-place.
        WarSnake.PlacementNew((WarSnake*)snakePtr, snakeData.Health, lenght, capacity, body1D);
    }

    private static void UnrollSnakeBody(int length, ushort* body1D, Snake snakeData, int boardWidth)
    {
        var sourceBody = snakeData.Body;
        for (var i = 0; i < length; i++)
        {
            body1D[i] = To1D(sourceBody[i], boardWidth);
        }
    }

    private static ushort To1D(Coordinate coord, int width) => (ushort)(coord.Y * width + coord.X);

    /// <summary>
    /// Ottiene un puntatore al tuo serpente. Accesso O(1).
    /// </summary>
    public WarSnake* GetMySnake() => (WarSnake*)_snakePointers[0];

    /// <summary>
    /// Ottiene un puntatore a un serpente specifico tramite il suo indice nel buffer.
    /// ATTENZIONE: l'indice non corrisponde all'ordine della richiesta API per gli avversari.
    /// </summary>
    public WarSnake* GetSnake(int index) => (WarSnake*)_snakePointers[index];

    public void Dispose()
    {
        if (_snakesMemory != null)
        {
            NativeMemory.AlignedFree(_snakesMemory);
        }
    }
}