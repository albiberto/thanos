// Thanos/BattleArena.cs

using System.Numerics;
using System.Runtime.InteropServices;
using Thanos.Enums;
using Thanos.SourceGen;

namespace Thanos.War;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WarArena : IDisposable
{
    private readonly byte* _snakesMemory;
    private readonly byte* _fieldMemory;
    private readonly byte* _boardMemory;
    
    private fixed long _snakePointers[Constants.MaxSnakes];
    private WarField* _field;
    
    public WarArena(Request request)
    {
        var board = request.Board;
        var height = board.Height;
        var width = board.Width;

        var activeSnakes = (uint)board.Snakes.Length;

        var area = width * height;

        // --- Step 1: Calculate Memory Layout ---
        var idealBodyCapacity = (int)BitOperations.RoundUpToPowerOf2(area);
        var realBodyCapacity = Math.Min(idealBodyCapacity, Constants.MaxBodyLength); // Cap the capacity at 256 (4 Cache Line).
        var snakeStride = (uint)(WarSnake.HeaderSize + realBodyCapacity * sizeof(ushort)); // Calculate byte sizes and the final stride for a single snake.
        
        // --- Step 2: Allocate Memory ---
        _boardMemory = (byte*)NativeMemory.AlignedAlloc(area * sizeof(byte), Constants.CacheLineSize);
        
        var snakesMemorySize = snakeStride * activeSnakes;
        _snakesMemory = (byte*)NativeMemory.AlignedAlloc(snakesMemorySize, Constants.CacheLineSize);
        
        var fieldMemorySize = area * sizeof(byte);
        _fieldMemory = (byte*)NativeMemory.AlignedAlloc(fieldMemorySize, Constants.CacheLineSize);
        
        // --- Step 3: Initialize the WarSnakes ---
        PlacementNewSnake(request, activeSnakes, realBodyCapacity, snakeStride);

        // --- Step 4: Initialize the WarField ---
        _field = (WarField*)_fieldMemory;
    }

    private void PlacementNewSnake(Request request, uint activeSnakes, int capacity, uint snakeStride)
    {
        var me = request.You;
        
        WarSnake.PlacementNew((WarSnake*)_snakePointers[0], me.Health, me.Length, capacity, me.Body.AsSpan());
        
        var myId = me.Id;
        
        
        for (byte i = 0; i < activeSnakes; i++)
        {
            var snakePtr = _snakesMemory + i * snakeStride;
            _snakePointers[i] = (long)snakePtr;
        }
    }
    
    private static int To1D(Coordinate coord, int width) => coord.Y * width + coord.X;

    private WarSnake* GetSnake(int index) => (WarSnake*)_snakePointers[index];

    public void Dispose()
    {
        NativeMemory.AlignedFree(_snakesMemory);
        NativeMemory.AlignedFree(_fieldMemory);
    }
}