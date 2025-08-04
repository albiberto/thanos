using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Thanos.Enums;

namespace Thanos;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct BattleField : IDisposable
{
    private byte* _grid;
    private ushort _boardSize;
    
    public byte this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _grid[index];
    }
    
    public void Initialize(ushort boardSize)
    {
        _boardSize = boardSize;
        var vectorSize = Vector<byte>.Count;
        var totalAllocatedMemory = (nuint)((boardSize + vectorSize - 1) / vectorSize * vectorSize);
        _grid = (byte*)NativeMemory.AlignedAlloc(totalAllocatedMemory, Constants.CacheLineSize);
    }

    public void ProjectBattlefield(Tesla* tesla, int maxSnakes)
    {
        for (byte i = 0; i < maxSnakes; i++)
        {
            var snake = tesla->GetSnake(i);
            if (snake->Health <= 0) continue;

            var snakeId = (byte)(i + 1);
            _grid[snake->Head] = snakeId;

            var bodyIndex = snake->TailIndex;
            for (var j = 0; j < snake->Length - 1; j++)
            {
                var bodyPos = snake->Body[bodyIndex];
                _grid[bodyPos] = snakeId;
                bodyIndex = (bodyIndex + 1) % snake->Capacity;
            }
        }
    }

    public void ApplyHazards(ReadOnlySpan<ushort> hazardPositions)
    {
        foreach (var position in hazardPositions) _grid[position] = Constants.Hazard;
    }
    
    public void ApplyFood(ReadOnlySpan<ushort> foodPositions)
    {
        foreach (var position in foodPositions) _grid[position] = Constants.Food;
    }
 
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear() => Unsafe.InitBlock(_grid, Constants.Empty, _boardSize);

    public void Dispose()
    {
        if (_grid == null) return;
        NativeMemory.AlignedFree(_grid);
        _grid = null;
    }
}