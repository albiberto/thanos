using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Thanos.Enums;

namespace Thanos;

[StructLayout(LayoutKind.Sequential, Pack = 64)]
public unsafe struct BattleSnake
{
    public const int HeaderSize = 64;

    // === CACHE LINE 1 - HEADER ===
    public int Health;
    public int Length;
    public ushort Head;
    public ushort Capacity;
    public int HeadIndex;
    public int TailIndex;
    
    private fixed byte _padding[44];

    // === CACHE LINE 2+ - BODY ARRAY (Buffer Circolare) ===
    public fixed ushort Body[1];

    public void Reset(ushort head, ushort capacity)
    {
        Health = 100;
        Length = 1;
        Head = head;
        Capacity = capacity;
        HeadIndex = 0;
        TailIndex = 0;
        Body[0] = head;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Move(ushort newHeadPosition, byte content, int hazardDamage)
    {
        var hasEaten = false;

        switch (content)
        {
            case >= Constants.Me and <= Constants.Enemy8: Health = 0; return false;
            case Constants.Food: Health = 100; hasEaten = true; break;
            case Constants.Hazard: Health -= hazardDamage; break;
            default: Health -= 1; break;
        }

        if (Health <= 0) return false;

        var oldHead = Head;
        Head = newHeadPosition;

        var nextHeadIndex = (HeadIndex + 1) % Capacity;

        if (hasEaten)
        {
            Length++;
        }
        else
        {
            TailIndex = (TailIndex + 1) % Capacity;
        }
        
        Body[nextHeadIndex] = oldHead;
        HeadIndex = nextHeadIndex;

        return true;
    }
}