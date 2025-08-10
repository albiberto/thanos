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
    private int _capacity;
    private int _nextHeadIndex;

    public int Health;
    public int Length;
    public ushort Head;
    public int TailIndex;

    private fixed byte _padding[PaddingSize];

    // === CACHE LINE 2+: BODY ===
    public fixed ushort Body[1];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void PlacementNew(WarSnake* snakePtr, ushort* bodyPtr, int length, int health, int capacity)
    {
        snakePtr->Health = health;
        snakePtr->Length = length;
        snakePtr->_capacity = capacity;
        
        // La testa è l'ULTIMO elemento del buffer invertito
        snakePtr->Head = bodyPtr[length - 1];

        // Copia il corpo invertito nel buffer
        Unsafe.CopyBlock(snakePtr->Body, bodyPtr, (uint)(length * sizeof(ushort)));

        // Il segmento più vecchio (la coda) è ora all'inizio del buffer
        snakePtr->TailIndex = 0;
        
        // Il prossimo segmento verrà scritto dopo la fine del blocco attuale
        snakePtr->_nextHeadIndex = length & (capacity - 1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Move(ushort newHeadPosition, bool hasEaten, int damage = 0)
    {
        ref var health = ref Health;
        ref var length = ref Length;
        ref var tailIndex = ref TailIndex;
        ref var nextHeadIndex = ref _nextHeadIndex;
        ref var capacity = ref _capacity;

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