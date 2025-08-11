using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Thanos.Enums;
using Thanos.SourceGen;

namespace Thanos.War;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WarSnake
{
    private const int PaddingSize = Constants.SizeOfCacheLine - sizeof(int) * 5 - sizeof(ushort) * 1;
    public const int SizeOfHeader = 64;
    
    // --- Header ---
    private int _capacity;
    private int _nextHeadIndex;

    public int Health;
    public int Length;
    public ushort Head;
    public int TailIndex;

    private fixed byte _padding[PaddingSize];

    // --- Body ---
    public fixed ushort Body[1];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Initialize(in Snake snakeDto, in WarField field, int capacity)
    {
        var length = Math.Min(snakeDto.Length, capacity);
        var sourceBody = snakeDto.Body.AsSpan();

        Health = snakeDto.Health;
        Length = length;
        _capacity = capacity;
        TailIndex = 0;
        _nextHeadIndex = length & (capacity - 1);

        for (var i = 0; i < length; i++)
        {
            var coord1D = field.To1D(in sourceBody[length - 1 - i]);
            Body[i] = coord1D;
            field.SetSnakeBit(coord1D);
        }
    
        Head = length > 0 ? Body[length - 1] : ushort.MaxValue;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Move(ushort newHeadPosition, bool hasEaten, int damage = 0)
    {
        if (hasEaten)
        {
            Health = 100;
        }
        else
        {
            Health -= damage + 1;
        }

        if (Dead) return;
        
        var capacityMask = _capacity - 1;

        Body[_nextHeadIndex] = Head;
        Head = newHeadPosition;
        _nextHeadIndex = (_nextHeadIndex + 1) & capacityMask;

        if (hasEaten && Length < _capacity)
        {
            Length++;
        }
        else
        {
            TailIndex = (TailIndex + 1) & capacityMask;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort GetTailPosition() => Body[TailIndex];
    
    public readonly bool Dead => Health <= 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Kill() => Health = 0;
}