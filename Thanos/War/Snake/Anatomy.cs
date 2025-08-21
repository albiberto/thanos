namespace Thanos.War.Snake;

public struct Anatomy(ushort head, ushort tail, int capacity, int length, int nextHeadIndex, int tailIndex = 0)
{
    public ushort Head { get; private set; } = head;
    public ushort Tail { get; private set; } = tail;
    public int NextHeadIndex { get; private set; } = nextHeadIndex;
    public int TailIndex { get; private set; } = tailIndex;
    public int Capacity { get; } = capacity;
    public int Length { get; private set; } = length;

    public void PushHead(ushort newHead)
    {
        Head = newHead;
        NextHeadIndex = (NextHeadIndex + 1) & (Capacity - 1);
    }

    public void PopTail() => TailIndex = (TailIndex + 1) & (Capacity - 1);

    public void IncrementLength()
    {
        if (Length < Capacity) Length++;
    }
}