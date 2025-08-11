using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Thanos.Enums;
using Thanos.SourceGen;

namespace Thanos.War;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WarArena : IDisposable
{
    private readonly uint _activeSnakes;

    private readonly byte* _snakesMemory;
    private readonly ulong* _fieldMemory;

    private fixed long _snakePointers[Constants.MaxSnakes];

    public WarArena(in Request request)
    {
        ref readonly var board = ref request.Board;
        
        var width = board.Width;
        var area = board.Area;
        var capacity = board.Capacity;
        
        _activeSnakes = (uint)board.Snakes.Length;
        
        // --- Calcolo Memoria per i Bitboard ---
        var bitboardSize = (area + 63) / 64 * sizeof(ulong);
        var bitboardsMemorySize = bitboardSize * WarField.TotalBitboards;
        
        // --- Calcolo Memoria per i Serpenti ---
        var snakeSize = WarSnake.HeaderSize + capacity * sizeof(ushort);
        var snakesMemorySize = snakeSize * _activeSnakes;

        // --- Allochiamo la memoria allineata ---
        _fieldMemory =  (ulong*)NativeMemory.AlignedAlloc(bitboardsMemorySize, Constants.CacheLineSize);
        _snakesMemory =  (byte*)NativeMemory.AlignedAlloc(snakesMemorySize, Constants.CacheLineSize);  
        
        // --- Inizializziamo la plancia di gioco (WarField) con cibo e hazards ---
        var field = new WarField(_fieldMemory, width, bitboardsMemorySize);
        field.AddFood(board.Food);
        field.AddHazard(board.Hazards);
        
        // --- Inizializziamo i serpenti sia sulla plancia di gioco (WarField) che nella struttura dedicata alla loro gestione (WarSnake) ---
        InitializeSnakes(in field, in request.You, board.Snakes, (int)capacity, snakeSize);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void InitializeSnakes(in WarField field, in Snake me, ReadOnlySpan<Snake> snakes, int capacity, uint snakeSize)
    {
        // Inizializza il tuo serpente a indice 0 (Me)
        InitializeSnake(field, me.Body, 0, me.Health, capacity, snakeSize);

        // Inizializza gli avversari a partire da indice 1 (Enemies)
        var myId = me.Id;
        byte opponentPointerIndex = 1;
        
        foreach (ref readonly var snake in snakes)
        {
            if (snake.Id == myId) continue;
        
            InitializeSnake(field, snake.Body, opponentPointerIndex, me.Health, capacity, snakeSize);
            opponentPointerIndex++;
        }
    }
    
    private void InitializeSnake(in WarField field, in ReadOnlySpan<Coordinate> Body, int index, int health, int capacity, uint snakeSize)
    {
        var length = Math.Min(Body.Length, capacity);
        var body1D = stackalloc ushort[length];
        for (var i = 0; i < length; i++) body1D[i] = field.To1D(in Body[length - 1 - i]);
        
        var snakePtr = (WarSnake*)(_snakesMemory + index * snakeSize);
        _snakePointers[index] = (long)snakePtr;
        
        WarSnake.PlacementNew((WarSnake*)_snakePointers[index], body1D, Body.Length, health, capacity);
        
        field.AddSnake(body1D, length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref WarSnake GetMySnake() => ref *(WarSnake*)_snakePointers[0];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref WarSnake GetSnake(int index) => ref *(WarSnake*)_snakePointers[index];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        if (_snakesMemory != null) NativeMemory.AlignedFree(_snakesMemory);
        if (_fieldMemory != null) NativeMemory.AlignedFree(_fieldMemory);
    }
}