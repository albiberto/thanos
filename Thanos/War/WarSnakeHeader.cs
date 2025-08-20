// using System.Runtime.InteropServices;
//
// namespace Thanos.War;
//
// [StructLayout(LayoutKind.Sequential)]
// public struct WarSnakeHeader(int index, int health, int capacity, int length, ushort head, int nextHeadIndex, int tailIndex)
// {
//     public int Index { get; } = index;
//     public int Health { get; private set; } = health;
//     public int Capacity { get; } = capacity;
//     public int Length { get; private set; } = length;
//     public ushort Head { get; private set; } = head;
//     public int NextHeadIndex { get; private set; } = nextHeadIndex;
//     public int TailIndex { get; private set; } = tailIndex;
//
//     public void Kill() => Health = 0;
//
//     public bool Damage(int amount)
//     {
//         Health -= amount;
//         return Dead;
//     }
//
//     public bool FullCure()
//     {
//         Health = 100;
//         return true;
//     }
//
//     public readonly bool Dead => Health <= 0;
//
//     public void PushHead(ushort newHead)
//     {
//         Head = newHead;
//         NextHeadIndex = (NextHeadIndex + 1) & (Capacity - 1);
//     }
//
//     public void PopTail() => TailIndex = (TailIndex + 1) & (Capacity - 1);
//
//     public void IncrementLength()
//     {
//         if (Length < Capacity) Length++;
//     }
// }