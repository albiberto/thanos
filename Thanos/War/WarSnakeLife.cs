using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace Thanos.War;

[StructLayout(LayoutKind.Sequential)]
public struct WarSnakeLife
{
    private const byte FullHealth = 100;

    public byte Credits { get; private set; }

    public byte Hp { get; private set; }

    public readonly bool IsDead => Hp <= 0;
    public readonly bool IsGrowthPending => Credits != 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetHp(byte hp)
    {
        Hp = hp;
        Credits = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Damage(byte amount)
    {
        if (Hp > amount) Hp -= amount;
        else Hp = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetPendingGrowth(byte credits) => Credits = credits;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ScheduleGrowth() => Credits++;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ConsumePendingGrowth()
    {
        if (Credits == 0) return false;
        Credits--;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Kill() => Hp = 0;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void FullCure() => Hp = FullHealth;
}