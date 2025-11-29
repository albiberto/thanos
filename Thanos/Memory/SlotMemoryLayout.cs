using System.Runtime.CompilerServices;
using Thanos.Common;
using Thanos.War;
using Thanos.War.Structures;

namespace Thanos.Memory;

public struct SlotMemoryLayout
{
    public readonly int WarSnakeLifeSize;
    public readonly int CircularQueueStateSize;
    public readonly int BitboardSize;
    public readonly int QueueBufferSize;

    public readonly int WarSnakeLifeOffset;
    public readonly int BitboardOffset;
    public readonly int CircularQueueStateOffset;
    public readonly int QueueBufferOffset;
    public readonly int SnakeStride;

    public readonly int SnakesBitboardOffset;
    public readonly int FoodBitboardOffset;
    public readonly int HazardsBitboardOffset;
    public readonly int SlotSize;

    public readonly ushort Capacity;

    public static SlotMemoryLayout Medium { get; } = new(Constants.Medium, 128, Constants.MaxSnakesCount
    );

    private SlotMemoryLayout(int area, ushort capacity, int snakeCount)
    {
        Capacity = capacity;

        WarSnakeLifeSize = Unsafe.SizeOf<WarSnakeLife>();
        BitboardSize = sizeof(ulong) * ((area + 63) / 64);
        CircularQueueStateSize = Unsafe.SizeOf<CircularQueueState>();
        QueueBufferSize = sizeof(ushort) * capacity;

        var relOffset = 0;
        WarSnakeLifeOffset = relOffset;
        relOffset += WarSnakeLifeSize;

        BitboardOffset = relOffset;
        relOffset += BitboardSize;

        CircularQueueStateOffset = relOffset;
        relOffset += CircularQueueStateSize;

        relOffset = relOffset.AlignUp64();
        QueueBufferOffset = relOffset;

        relOffset += QueueBufferSize;
        SnakeStride = relOffset;

        var globalOffset = SnakeStride * snakeCount;

        globalOffset = globalOffset.AlignUp64();

        SnakesBitboardOffset = globalOffset;
        globalOffset += BitboardSize;

        FoodBitboardOffset = globalOffset;
        globalOffset += BitboardSize;

        HazardsBitboardOffset = globalOffset;
        globalOffset += BitboardSize;

        SlotSize = globalOffset.AlignUp64();
    }
}