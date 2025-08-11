using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Thanos.Enums;

namespace Thanos.War;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WarSnake
{
    private const int PaddingSize = Constants.CacheLineSize - sizeof(int) * 5 - sizeof(ushort) * 1;
    public const int HeaderSize = Constants.CacheLineSize;

    // === CACHE LINE 1 ===
    public int Health;
    public int Length;
    public ushort Head;
    
    public int NextHeadIndex;
    public int TailIndex;
    public int Capacity;

    private fixed byte _padding[PaddingSize];

    // === CACHE LINE 2+: BODY ===
    public fixed ushort Body[1];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Move(ushort newHeadPosition, bool hasEaten, int damage = 0)
    {
        ref var health = ref Health;
        ref var length = ref Length;
        ref var tailIndex = ref TailIndex;
        ref var nextHeadIndex = ref NextHeadIndex;
        ref var capacity = ref Capacity;

        if (hasEaten)
        {
            health = 100;
        }
        else
        {
            health -= damage + 1;
        }

        if (Dead) return;

        var capacityMask = capacity - 1;

        Body[nextHeadIndex] = Head;
        Head = newHeadPosition;
        nextHeadIndex = (nextHeadIndex + 1) & capacityMask;

        if (hasEaten && length < capacity)
        {
            length++;
        }
        else
        {
            tailIndex = (tailIndex + 1) & capacityMask;
        }
    }

    public readonly bool Dead
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Health <= 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Kill() => Health = 0;
}