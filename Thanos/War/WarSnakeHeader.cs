using System.Runtime.InteropServices;

namespace Thanos.War;

[StructLayout(LayoutKind.Sequential)]
public struct WarSnakeHeader(int index, ushort head, int health, int length, int capacity)
{
    private ushort _head = head;
    private int _health = health;
    
    public int Index { get; } = index;
    public int Capacity { get; } = capacity;
    public int Length { get; private set; } = length;
    public int NextHeadIndex { get; private set; } = length & (capacity - 1);
    public int TailIndex { get; private set; } = 0;
    
    public bool FullCure()
    {
        _health = 100;
        return false;
    }
    
    public bool Damage(int amount)
    {
        _health -= amount;
        return Dead;
    }
    
    public void Kill() => _health = 0;
    
    public bool Dead => _health <= 0;

    public void PushHead(ushort newHead)
    {
        _head = newHead;
        NextHeadIndex = (NextHeadIndex + 1) & (Capacity - 1);
    }

    public void PopTail() => TailIndex = (TailIndex + 1) & (Capacity - 1);

    public void IncrementLength()
    {
        if (Length < Capacity) Length++;
    }
}