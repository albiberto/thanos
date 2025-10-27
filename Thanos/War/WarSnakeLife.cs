using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace Thanos.War;

[StructLayout(LayoutKind.Sequential)]
public struct WarSnakeLife
{
    private const byte FullHealth = 100;

    private byte _isPendingGrowth;

    public byte HP { get; private set; }

    public readonly bool IsDead => HP <= 0;
    public readonly bool IsGrowthPending => _isPendingGrowth != 0;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetHP(byte hp) => HP = hp;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Damage(byte amount)
    {
        if (HP > amount) HP -= amount;
        else HP = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ScheduleGrowth() => _isPendingGrowth = 1;

    public bool ConsumePendingGrowth()
    {
        if (_isPendingGrowth == 0) return false;
        _isPendingGrowth = 0;
        return true;
    }

    public void Kill() => HP = 0;
    public void FullCure() => HP = FullHealth;
}