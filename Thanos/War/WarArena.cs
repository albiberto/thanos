using System;
using System.Runtime.InteropServices;
using Thanos.Enums;
using Thanos.SourceGen;

namespace Thanos.War;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WarArena : IDisposable
{
    private readonly uint _activeSnakes;
    
    private readonly uint _snakeStride;
    private readonly byte* _snakesMemory;
    private readonly ulong* _fieldMemory;
    private readonly WarField _field;

    private fixed long _snakePointers[Constants.MaxSnakes];

    public WarArena(in Request request)
    {
        ref readonly var board = ref request.Board;
        ref readonly var me = ref request.You;
        
        _activeSnakes = (uint)board.Snakes.Length;

        // --- 1. Allocazione Memoria ---
        _fieldMemory = AllocateFieldMemory(board.Area);
        _snakesMemory = AllocateSnakesMemory(board.Capacity, _activeSnakes, out _snakeStride);
        
        // --- 2. Inizializzazione Specialisti ---
        _field = new WarField(_fieldMemory, board.Width, board.Area);
        _field.InitializeStaticBoards(in board);
        
        InitializeSnakes(me, in board, board.Capacity);
    }
    
    private void InitializeSnakes(in Snake me, in Board board, int capacity)
    {
        InitializeSingleSnake(in me, 0, capacity);

        byte opponentIndex = 1;
        foreach (ref readonly var snakeData in board.Snakes.AsSpan())
        {
            if (snakeData.Id == me.Id) continue;
            InitializeSingleSnake(in snakeData, opponentIndex, capacity);
            opponentIndex++;
        }
    }
    
    private void InitializeSingleSnake(in Snake snakeDto, byte snakeIndex, int capacity)
    {
        var snakePtr = (WarSnake*)(_snakesMemory + snakeIndex * _snakeStride);
        _snakePointers[snakeIndex] = (long)snakePtr;
        
        ref var warSnake = ref *snakePtr;
        warSnake.Initialize(in snakeDto, in _field, capacity);
    }

    private static ulong* AllocateFieldMemory(uint area)
    {
        var ulongsPerBitboard = (area + 63) / 64;
        var totalMemorySize = ulongsPerBitboard * WarField.TotalBitboards * sizeof(ulong);
        return (ulong*)NativeMemory.AlignedAlloc(totalMemorySize, Constants.CacheLineSize);
    }

    private static byte* AllocateSnakesMemory(int capacity, uint activeSnakes, out uint snakeStride)
    {
        snakeStride = (uint)(WarSnake.HeaderSize + capacity * sizeof(ushort));
        var snakesMemorySize = snakeStride * activeSnakes;
        return (byte*)NativeMemory.AlignedAlloc(snakesMemorySize, Constants.CacheLineSize);
    }

    public ref WarSnake GetMySnake() => ref *(WarSnake*)_snakePointers[0];
    public ref WarSnake GetSnake(int index) => ref *(WarSnake*)_snakePointers[index];
    public void Dispose()
    {
        if (_snakesMemory != null) NativeMemory.AlignedFree(_snakesMemory);
        if (_fieldMemory != null) NativeMemory.AlignedFree(_fieldMemory);
    }
}