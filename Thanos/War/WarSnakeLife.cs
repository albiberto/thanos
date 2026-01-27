using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace Thanos.War;

[StructLayout(LayoutKind.Sequential)]
public struct WarSnakeLife
{
    private const byte FullHealth = 100;

    private byte _credits;

    public byte Hp { get; private set; }

    public readonly bool IsDead => Hp <= 0;
    public readonly bool IsGrowthPending => _credits != 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetHp(byte hp) => Hp = hp;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Damage(byte amount)
    {
        if (Hp > amount) Hp -= amount;
        else Hp = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ScheduleGrowth() => _credits++;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ConsumePendingGrowth()
    {
        if (_credits == 0) return false;
        _credits--;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Kill() => Hp = 0;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void FullCure() => Hp = FullHealth;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ResetStack() => _credits = 0;
}