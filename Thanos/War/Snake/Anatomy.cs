namespace Thanos.War.Snake;

public struct Anatomy(int capacity, int length, int tailIndex = 0)
{
    public int TailIndex { get; private set; } = tailIndex;
    public int Length { get; private set; } = length;
    public int Capacity { get; } = capacity;

    public readonly int HeadIndex => (TailIndex + Length - 1) & (Capacity - 1);
    public readonly int NextHeadIndex => (TailIndex + Length) & (Capacity - 1);

    public void PopTail() => TailIndex = (TailIndex + 1) & (Capacity - 1);

    public void IncrementLength()
    {
        if (Length < Capacity) Length++;
    }
}