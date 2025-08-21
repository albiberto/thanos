namespace Thanos.War.Snake;

public struct Anatomy(int capacity, int length)
{
    public int TailIndex { get; private set; }
    public int Length { get; private set; } = length;
    public int Capacity { get; } = capacity;
    public int CapacityMask { get; } = capacity - 1;
    
    public bool IsFull => Length == Capacity;

    public readonly int HeadIndex => (TailIndex + Length - 1) & CapacityMask;
    public readonly int NextHeadIndex => (TailIndex + Length) & CapacityMask;
    
    public void PopTail() => TailIndex = (TailIndex + 1) & CapacityMask;

    public void IncrementLength()
    {
        if (Length < Capacity) Length++;
    }
}