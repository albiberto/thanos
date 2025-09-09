using System.Runtime.InteropServices;

namespace Thanos.War;

[StructLayout(LayoutKind.Sequential)]
public struct WarSnakeHeader
{
    private const byte FullHealth = 100;

    public ushort Length;
    public ushort Head;
    public ushort Tail;

    public byte Points;
    private byte _isPendingGrowth;

    public readonly bool IsDead => Points <= 0;
    public readonly bool IsGrowthPending => _isPendingGrowth == 1;
    public void ScheduleGrowth() => _isPendingGrowth = 1;

    public void ProcessPendingGrowth()
    {
        if (_isPendingGrowth == 0) return;

        Length++;
        _isPendingGrowth = 0;
    }

    public void Damage(byte amount)
    {
        if (Points > amount)
            Points -= amount;
        else
            Points = 0;
    }

    public void Kill() => Points = 0;

    public void FullCure() => Points = FullHealth;

    public void PlacementNew(ushort length, ushort head, ushort tail, byte points)
    {
        Length = length;
        Head = head;
        Tail = tail;
        Points = points;
        _isPendingGrowth = 0;
    }
}